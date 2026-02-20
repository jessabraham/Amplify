using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.AI;
using Amplify.Application.Common.Interfaces.Infrastructure;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amplify.Infrastructure.Services;

/// <summary>
/// Background service that periodically scans watchlist symbols for patterns.
/// Includes throttling controls to protect GPU/AI resources.
/// 
/// Configuration (appsettings.json):
///   "BackgroundScanner": {
///     "Enabled": true,
///     "TickIntervalSeconds": 60,
///     "MaxConcurrentAICalls": 1,
///     "DelayBetweenAICallsMs": 3000,
///     "PauseAIDuringMarketHours": false,
///     "MarketOpenHourUtc": 14,
///     "MarketCloseHourUtc": 21,
///     "MaxScansPerTick": 5
///   }
/// </summary>
public class BackgroundPatternScannerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundPatternScannerService> _logger;
    private readonly IConfiguration _config;

    // Semaphore to limit concurrent AI calls (protects GPU from being overwhelmed)
    private readonly SemaphoreSlim _aiSemaphore;

    public BackgroundPatternScannerService(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundPatternScannerService> logger,
        IConfiguration config)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _config = config;

        var maxConcurrent = _config.GetValue("BackgroundScanner:MaxConcurrentAICalls", 1);
        _aiSemaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = _config.GetValue("BackgroundScanner:Enabled", true);
        if (!enabled)
        {
            _logger.LogInformation("Background pattern scanner is DISABLED via config.");
            return;
        }

        _logger.LogInformation("Background pattern scanner started.");

        // Wait on startup to let the app fully initialize
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        var tickSeconds = _config.GetValue("BackgroundScanner:TickIntervalSeconds", 60);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ScanDueItemsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error in background pattern scanner tick.");
            }

            await Task.Delay(TimeSpan.FromSeconds(tickSeconds), stoppingToken);
        }

        _logger.LogInformation("Background pattern scanner stopped.");
    }

    /// <summary>
    /// Check if AI should be paused right now (market hours priority mode).
    /// </summary>
    private bool ShouldPauseAI()
    {
        var pauseDuringMarket = _config.GetValue("BackgroundScanner:PauseAIDuringMarketHours", false);
        if (!pauseDuringMarket) return false;

        var openHour = _config.GetValue("BackgroundScanner:MarketOpenHourUtc", 14);
        var closeHour = _config.GetValue("BackgroundScanner:MarketCloseHourUtc", 21);
        var nowUtc = DateTime.UtcNow.Hour;

        return nowUtc >= openHour && nowUtc < closeHour;
    }

    private async Task ScanDueItemsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var detector = scope.ServiceProvider.GetRequiredService<IPatternDetector>();
        var analyzer = scope.ServiceProvider.GetRequiredService<IPatternAnalyzer>();
        var notifier = scope.ServiceProvider.GetRequiredService<IScanNotificationService>();

        var now = DateTime.UtcNow;
        var maxPerTick = _config.GetValue("BackgroundScanner:MaxScansPerTick", 5);
        var aiPaused = ShouldPauseAI();

        var dueItems = await context.Set<WatchlistItem>()
            .Where(w => w.IsActive)
            .Where(w => w.LastScannedAt == null
                || EF.Functions.DateDiffMinute(w.LastScannedAt.Value, now) >= w.ScanIntervalMinutes)
            .Include(w => w.User)
            .OrderBy(w => w.LastScannedAt ?? DateTime.MinValue)
            .Take(maxPerTick)
            .ToListAsync(ct);

        if (dueItems.Count == 0) return;

        _logger.LogInformation(
            "Scanning {Count} watchlist items (max {Max}/tick, AI {AIStatus}).",
            dueItems.Count, maxPerTick, aiPaused ? "PAUSED" : "active");

        var delayBetweenMs = _config.GetValue("BackgroundScanner:DelayBetweenAICallsMs", 3000);

        foreach (var item in dueItems)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var useAI = item.EnableAI && !aiPaused;
                await ScanSingleItemAsync(item, detector, analyzer, notifier, context, useAI, ct);

                if (useAI && delayBetweenMs > 0)
                    await Task.Delay(delayBetweenMs, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan {Symbol} for user {UserId}.", item.Symbol, item.UserId);
            }
        }

        await context.SaveChangesAsync(ct);
    }

    private async Task ScanSingleItemAsync(
        WatchlistItem item,
        IPatternDetector detector,
        IPatternAnalyzer analyzer,
        IScanNotificationService notifier,
        ApplicationDbContext context,
        bool useAI,
        CancellationToken ct)
    {
        var candles = GenerateSampleCandles(item.Symbol, 250);
        if (candles.Count == 0) return;

        // Detect patterns (math-only, always fast)
        var patterns = detector.DetectAll(candles);

        if (item.MinConfidence > 0)
            patterns = patterns.Where(p => p.Confidence >= item.MinConfidence).ToList();

        // Run AI synthesis with semaphore to prevent GPU flooding
        MultiPatternAnalysis? aiSynthesis = null;
        if (patterns.Any() && useAI)
        {
            await _aiSemaphore.WaitAsync(ct);
            try
            {
                aiSynthesis = await analyzer.SynthesizePatternsAsync(patterns, candles, item.Symbol);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "AI analysis unavailable for {Symbol}.", item.Symbol);
            }
            finally
            {
                _aiSemaphore.Release();
            }
        }

        // Update watchlist item metadata
        item.LastScannedAt = DateTime.UtcNow;
        item.LastPatternCount = patterns.Count;
        item.LastBias = aiSynthesis?.OverallBias;
        item.UpdatedAt = DateTime.UtcNow;

        // Save patterns to DB
        foreach (var p in patterns)
        {
            var verdict = aiSynthesis?.PatternVerdicts.FirstOrDefault(v =>
                v.PatternName.Equals(p.PatternName, StringComparison.OrdinalIgnoreCase));

            context.DetectedPatterns.Add(new DetectedPattern
            {
                Asset = item.Symbol,
                PatternType = p.PatternType,
                Direction = p.Direction,
                Timeframe = PatternTimeframe.Daily,
                Confidence = p.Confidence,
                HistoricalWinRate = p.HistoricalWinRate,
                Description = p.Description,
                DetectedAtPrice = candles.Last().Close,
                SuggestedEntry = p.SuggestedEntry,
                SuggestedStop = p.SuggestedStop,
                SuggestedTarget = p.SuggestedTarget,
                PatternStartDate = p.StartDate,
                PatternEndDate = p.EndDate,
                AIAnalysis = verdict is not null ? $"[{verdict.Grade}] {verdict.OneLineReason}" : null,
                AIConfidence = aiSynthesis?.OverallConfidence,
                UserId = item.UserId
            });
        }

        // Build notification
        var topPattern = patterns.OrderByDescending(p => p.Confidence).FirstOrDefault();
        var isAlert = topPattern is not null
            && topPattern.Confidence >= 75
            && (aiSynthesis?.OverallConfidence ?? 0) >= 70;

        var notification = new ScanNotification
        {
            Symbol = item.Symbol,
            PatternCount = patterns.Count,
            CurrentPrice = candles.Last().Close,
            OverallBias = aiSynthesis?.OverallBias,
            AIConfidence = aiSynthesis?.OverallConfidence,
            RecommendedAction = aiSynthesis?.RecommendedAction,
            TopPattern = topPattern?.PatternName,
            TopPatternConfidence = topPattern?.Confidence,
            ScannedAt = DateTime.UtcNow,
            IsAlert = isAlert,
            AlertMessage = isAlert
                ? $"🔔 {item.Symbol}: {topPattern!.PatternName} ({topPattern.Confidence:F0}%) — AI says {aiSynthesis?.RecommendedAction}"
                : null
        };

        // Push via abstraction (SignalR under the hood)
        await notifier.NotifyScanCompletedAsync(item.UserId, notification);

        if (isAlert)
            await notifier.NotifyPatternAlertAsync(item.UserId, notification);

        _logger.LogInformation(
            "Scanned {Symbol}: {Count} patterns, bias={Bias}, AI={AI}",
            item.Symbol, patterns.Count, aiSynthesis?.OverallBias ?? "N/A", useAI ? "yes" : "skipped");
    }

    private List<Candle> GenerateSampleCandles(string symbol, int count)
    {
        var candles = new List<Candle>();
        var random = new Random(symbol.GetHashCode() + DateTime.UtcNow.DayOfYear);

        var basePrice = symbol.ToUpper() switch
        {
            "TSLA" => 410m,
            "AAPL" => 225m,
            "MSFT" => 420m,
            "GOOGL" => 175m,
            "AMZN" => 200m,
            "NVDA" => 870m,
            "META" => 590m,
            "BTC" => 95000m,
            _ => 150m
        };

        var price = basePrice * 0.85m;

        for (int i = count; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;

            var change = (decimal)(random.NextDouble() - 0.47) * basePrice * 0.02m;
            var open = price;
            var close = price + change;
            var high = Math.Max(open, close) + (decimal)random.NextDouble() * basePrice * 0.008m;
            var low = Math.Min(open, close) - (decimal)random.NextDouble() * basePrice * 0.008m;
            var volume = 5000000m + (decimal)random.Next(0, 15000000);

            candles.Add(new Candle
            {
                Time = date,
                Open = Math.Round(open, 2),
                High = Math.Round(high, 2),
                Low = Math.Round(low, 2),
                Close = Math.Round(close, 2),
                Volume = volume
            });
            price = close;
        }
        return candles;
    }
}