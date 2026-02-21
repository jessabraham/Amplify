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
                await RefreshPositionPricesAsync(stoppingToken);
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
                await ScanSingleItemAsync(item, detector, analyzer, notifier, context, scope, useAI, ct);

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
        IServiceScope scope,
        bool useAI,
        CancellationToken ct)
    {
        // Use IMarketDataService (Alpaca → sample data fallback)
        var marketData = scope.ServiceProvider
            .GetRequiredService<Amplify.Application.Common.Interfaces.Market.IMarketDataService>();
        var candles = await marketData.GetCandlesAsync(item.Symbol, 250, "Daily");
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

        // ═══════════════════════════════════════════════════════════════
        // AUTO-CREATE SIMULATED TRADE for high-confidence AI signals
        // This closes the loop: scan → detect → AI analyze → paper trade → track → learn
        // ═══════════════════════════════════════════════════════════════
        if (isAlert && aiSynthesis is not null
            && aiSynthesis.RecommendedEntry.HasValue
            && aiSynthesis.RecommendedStop.HasValue
            && aiSynthesis.RecommendedTarget.HasValue
            && aiSynthesis.RecommendedEntry.Value > 0)
        {
            try
            {
                var simulation = scope.ServiceProvider.GetRequiredService<TradeSimulationService>();

                // Determine direction from AI bias
                var isLong = aiSynthesis.OverallBias?.Contains("Bullish", StringComparison.OrdinalIgnoreCase) == true
                    || aiSynthesis.RecommendedAction?.Contains("Buy", StringComparison.OrdinalIgnoreCase) == true
                    || aiSynthesis.RecommendedAction?.Contains("Long", StringComparison.OrdinalIgnoreCase) == true;

                var direction = isLong ? SignalType.Long : SignalType.Short;

                // Check we haven't already created a trade for this symbol recently (within 24h)
                var recentTrade = await context.SimulatedTrades
                    .AnyAsync(t => t.Asset == item.Symbol
                        && t.UserId == item.UserId
                        && t.Status == SimulationStatus.Active
                        && t.CreatedAt > DateTime.UtcNow.AddHours(-24), ct);

                if (!recentTrade)
                {
                    // Create TradeSignal first (the signal is the parent record)
                    var signal = new TradeSignal
                    {
                        Asset = item.Symbol,
                        SignalType = direction,
                        Source = SignalSource.AI,
                        Status = SignalStatus.Pending,
                        EntryPrice = aiSynthesis.RecommendedEntry.Value,
                        StopLoss = aiSynthesis.RecommendedStop.Value,
                        Target1 = aiSynthesis.RecommendedTarget.Value,
                        Regime = MarketRegime.Trending,
                        PatternName = topPattern?.PatternType.ToString(),
                        PatternTimeframe = "Daily",
                        PatternConfidence = topPattern?.Confidence,
                        AIConfidence = aiSynthesis.OverallConfidence,
                        AIRecommendedAction = aiSynthesis.RecommendedAction,
                        AISummary = aiSynthesis.Summary,
                        AIBias = aiSynthesis.OverallBias,
                        SetupScore = aiSynthesis.OverallConfidence,
                        UserId = item.UserId
                    };
                    context.TradeSignals.Add(signal);
                    await context.SaveChangesAsync(ct);

                    // Create SimulatedTrade linked to the signal
                    var simTrade = await simulation.CreateFromSignalAsync(signal, new
                    {
                        PatternType = topPattern!.PatternType.ToString(),
                        PatternDirection = topPattern.Direction.ToString(),
                        PatternTimeframe = "Daily",
                        PatternConfidence = topPattern.Confidence
                    });

                    // ═══════════════════════════════════════════════════════
                    // AI AUTO-TRADING: Create real Position if budget > 0
                    // ═══════════════════════════════════════════════════════
                    await TryCreateAiPositionAsync(context, item, signal, aiSynthesis, direction, ct);

                    _logger.LogInformation(
                        "🎯 Auto-created simulated trade for {Symbol}: {Direction} at {Entry}, stop {Stop}, target {Target} (AI: {Confidence}%)",
                        item.Symbol, direction, aiSynthesis.RecommendedEntry, aiSynthesis.RecommendedStop,
                        aiSynthesis.RecommendedTarget, aiSynthesis.OverallConfidence);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-create simulated trade for {Symbol}", item.Symbol);
            }
        }

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

    /// <summary>
    /// Refresh current prices for all open positions using the latest candle close.
    /// Runs on each tick before pattern scanning.
    /// </summary>
    private async Task RefreshPositionPricesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var marketData = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.Market.IMarketDataService>();

        var openPositions = await context.Positions
            .Where(p => p.Status == Domain.Enumerations.PositionStatus.Open && p.IsActive)
            .ToListAsync(ct);

        if (!openPositions.Any()) return;

        // Group by symbol to avoid duplicate API calls
        var symbols = openPositions.Select(p => p.Symbol).Distinct().ToList();
        var priceCache = new Dictionary<string, decimal>();

        foreach (var symbol in symbols)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // Get 1 latest daily candle to get current close price
                var candles = await marketData.GetCandlesAsync(symbol, 1, "1H");
                if (candles.Count > 0)
                {
                    priceCache[symbol] = candles.Last().Close;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to refresh price for {Symbol}", symbol);
            }
        }

        // Update all positions with fresh prices
        var updated = 0;
        foreach (var position in openPositions)
        {
            if (priceCache.TryGetValue(position.Symbol, out var latestPrice) && latestPrice > 0)
            {
                position.CurrentPrice = latestPrice;

                // Recalculate unrealized P&L
                var direction = position.SignalType == Domain.Enumerations.SignalType.Short ? -1 : 1;
                position.UnrealizedPnL = direction * (latestPrice - position.EntryPrice) * position.Quantity;

                updated++;
            }
        }

        if (updated > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Refreshed prices for {Count} open positions", updated);
        }
    }

    /// <summary>
    /// If the user has an AI trading budget > 0%, create a real Position for the AI signal.
    /// Respects budget limits, avoids duplicates, and sizes positions within the allocated budget.
    /// </summary>
    private async Task TryCreateAiPositionAsync(
        ApplicationDbContext context,
        WatchlistItem item,
        TradeSignal signal,
        dynamic aiSynthesis,
        SignalType direction,
        CancellationToken ct)
    {
        try
        {
            var user = await context.Users
                .OfType<Domain.Entities.Identity.ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == item.UserId, ct);

            if (user is null || user.AiTradingBudgetPercent <= 0)
                return; // Simulation only — no real positions

            // Calculate AI budget
            var realizedPnL = 0m;
            try
            {
                realizedPnL = await context.SimulatedTrades
                    .Where(t => t.UserId == item.UserId && t.Status == SimulationStatus.Resolved)
                    .SumAsync(t => t.PnLDollars ?? 0, ct);
            }
            catch { }

            var totalInvested = await context.Positions
                .Where(p => p.UserId == item.UserId && p.Status == PositionStatus.Open && p.IsActive)
                .SumAsync(p => p.EntryPrice * p.Quantity, ct);

            var cashAvailable = user.StartingCapital + realizedPnL - totalInvested;
            var aiBudgetTotal = cashAvailable * user.AiTradingBudgetPercent / 100m;

            var aiInvested = await context.Positions
                .Where(p => p.UserId == item.UserId && p.Status == PositionStatus.Open && p.IsActive && p.IsAiGenerated)
                .SumAsync(p => p.EntryPrice * p.Quantity, ct);

            var aiCashRemaining = aiBudgetTotal - aiInvested;

            if (aiCashRemaining <= 0)
            {
                _logger.LogInformation("AI budget exhausted for user {UserId} — skipping position for {Symbol}", item.UserId, item.Symbol);
                return;
            }

            // Check no existing open AI position for this symbol
            var existingPosition = await context.Positions
                .AnyAsync(p => p.UserId == item.UserId && p.Symbol == item.Symbol
                    && p.Status == PositionStatus.Open && p.IsActive && p.IsAiGenerated, ct);

            if (existingPosition)
            {
                _logger.LogDebug("AI already has open position for {Symbol} — skipping", item.Symbol);
                return;
            }

            // Size the position: use max 25% of AI budget per position, capped by remaining
            var maxPerPosition = aiBudgetTotal * 0.25m;
            var positionBudget = Math.Min(maxPerPosition, aiCashRemaining);

            if (positionBudget < 10) return; // Too small to bother

            var entryPrice = signal.EntryPrice;
            if (entryPrice <= 0) return;

            // Determine asset class
            var isCrypto = item.Symbol.EndsWith("USD", StringComparison.OrdinalIgnoreCase)
                && !item.Symbol.Contains(".") && item.Symbol.Length <= 10;

            var quantity = isCrypto
                ? Math.Round(positionBudget / entryPrice, 6)
                : Math.Floor(positionBudget / entryPrice);
            if (quantity <= 0) return;

            

            var position = new Position
            {
                Symbol = item.Symbol,
                AssetClass = isCrypto ? AssetClass.Crypto : AssetClass.Stock,
                SignalType = direction,
                EntryPrice = entryPrice,
                Quantity = quantity,
                StopLoss = signal.StopLoss,
                Target1 = signal.Target1,
                CurrentPrice = entryPrice,
                Status = PositionStatus.Open,
                IsAiGenerated = true,
                TradeSignalId = signal.Id,
                Notes = $"AI auto-trade: {signal.PatternName} ({signal.AIConfidence:F0}% confidence)",
                UserId = item.UserId
            };

            context.Positions.Add(position);
            await context.SaveChangesAsync(ct);

            _logger.LogInformation(
                "🤖 AI created real position: {Symbol} {Direction} {Qty} units @ {Entry} (budget: {Budget:C0}, remaining: {Remaining:C0})",
                item.Symbol, direction, quantity, entryPrice, positionBudget, aiCashRemaining - (quantity * entryPrice));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create AI position for {Symbol}", item.Symbol);
        }
    }
}