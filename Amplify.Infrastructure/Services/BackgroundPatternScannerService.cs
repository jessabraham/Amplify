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
        var _lifecycleTick = 0;
        var _advisorTick = 0;
        var lifecycleInterval = 5;  // every 5 ticks (5 min at 60s tick)
        var advisorIntervalHours = _config.GetValue("BackgroundScanner:AdvisorIntervalHours", 6);
        var advisorInterval = advisorIntervalHours * 60; // convert to ticks (at 60s tick)

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Every tick: refresh prices + scan patterns
                await RefreshPositionPricesAsync(stoppingToken);
                await ScanDueItemsAsync(stoppingToken);

                _lifecycleTick++;
                _advisorTick++;

                // Every 5 minutes: update pattern lifecycle
                if (_lifecycleTick >= lifecycleInterval)
                {
                    _lifecycleTick = 0;
                    await UpdatePatternLifecyclesAsync(stoppingToken);
                }

                // Every N hours: auto-run portfolio advisor
                if (_advisorTick >= advisorInterval)
                {
                    _advisorTick = 0;
                    await AutoRunPortfolioAdvisorAsync(stoppingToken);
                }
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

        // ── Scan multiple timeframes ──────────────────────────────────
        var timeframesToScan = new[] { ("1H", 200, PatternTimeframe.OneHour), ("4H", 200, PatternTimeframe.FourHour), ("Daily", 250, PatternTimeframe.Daily), ("Weekly", 104, PatternTimeframe.Weekly) };
        var allPatterns = new List<(Application.Common.Models.PatternResult Pattern, PatternTimeframe Timeframe, decimal LastClose)>();
        List<Application.Common.Models.Candle> dailyCandles = new();

        foreach (var (tf, count, ptf) in timeframesToScan)
        {
            try
            {
                var candles = await marketData.GetCandlesAsync(item.Symbol, count, tf);
                if (candles.Count == 0) continue;

                if (tf == "Daily") dailyCandles = candles;

                var patterns = detector.DetectAll(candles);
                if (item.MinConfidence > 0)
                    patterns = patterns.Where(p => p.Confidence >= item.MinConfidence).ToList();

                // Apply timeframe confidence scaling (same as manual scanner)
                foreach (var p in patterns)
                {
                    p.Timeframe = tf;
                    p.Confidence = ptf switch
                    {
                        PatternTimeframe.OneHour => Math.Min(p.Confidence * 0.85m, 95),
                        PatternTimeframe.FourHour => Math.Min(p.Confidence * 0.90m, 95),
                        PatternTimeframe.Weekly => Math.Min(p.Confidence * 1.05m, 98),
                        _ => p.Confidence
                    };
                }

                foreach (var p in patterns)
                    allPatterns.Add((p, ptf, candles.Last().Close));
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to fetch {Timeframe} candles for {Symbol}", tf, item.Symbol);
            }
        }

        if (!allPatterns.Any()) return;

        // Use daily candles for AI synthesis (most balanced view)
        var candlesForAI = dailyCandles.Any() ? dailyCandles : allPatterns.Select(a => a.Pattern).ToList() is { } _ ? new List<Application.Common.Models.Candle>() : new();
        var patternsForAI = allPatterns.Select(a => a.Pattern).ToList();

        // Run AI synthesis with semaphore to prevent GPU flooding
        MultiPatternAnalysis? aiSynthesis = null;
        if (patternsForAI.Any() && useAI && candlesForAI.Any())
        {
            await _aiSemaphore.WaitAsync(ct);
            try
            {
                aiSynthesis = await analyzer.SynthesizePatternsAsync(patternsForAI, candlesForAI, item.Symbol);
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
        item.LastPatternCount = allPatterns.Count;
        item.LastBias = aiSynthesis?.OverallBias;
        item.UpdatedAt = DateTime.UtcNow;

        // Save patterns to DB (with dedup — skip if same pattern+symbol+timeframe already active)
        var existingActive = await context.DetectedPatterns
            .Where(dp => dp.Asset == item.Symbol
                && dp.UserId == item.UserId
                && (dp.Status == PatternStatus.Active || dp.Status == PatternStatus.PlayingOut))
            .Select(dp => new { dp.PatternType, dp.Timeframe })
            .ToListAsync();

        var existingSet = new HashSet<string>(
            existingActive.Select(e => $"{e.PatternType}|{e.Timeframe}"));

        var newPatternsAdded = 0;
        foreach (var (p, ptf, lastClose) in allPatterns)
        {
            var key = $"{p.PatternType}|{ptf}";
            if (existingSet.Contains(key))
                continue; // Skip — already have an active pattern of this type+timeframe

            existingSet.Add(key); // Prevent duplicates within this batch too

            var verdict = aiSynthesis?.PatternVerdicts.FirstOrDefault(v =>
                v.PatternName.Equals(p.PatternName, StringComparison.OrdinalIgnoreCase));

            context.DetectedPatterns.Add(new DetectedPattern
            {
                Asset = item.Symbol,
                PatternType = p.PatternType,
                Direction = p.Direction,
                Timeframe = ptf,
                Confidence = p.Confidence,
                HistoricalWinRate = p.HistoricalWinRate,
                Description = p.Description,
                DetectedAtPrice = lastClose,
                SuggestedEntry = p.SuggestedEntry,
                SuggestedStop = p.SuggestedStop,
                SuggestedTarget = p.SuggestedTarget,
                PatternStartDate = p.StartDate,
                PatternEndDate = p.EndDate,
                AIAnalysis = verdict is not null ? $"[{verdict.Grade}] {verdict.OneLineReason}" : null,
                AIConfidence = aiSynthesis?.OverallConfidence,
                Status = PatternStatus.Active,
                ExpiresAt = DetectedPattern.CalculateExpiry(ptf, DateTime.UtcNow),
                CurrentPrice = lastClose,
                UserId = item.UserId
            });
            newPatternsAdded++;
        }

        if (newPatternsAdded < allPatterns.Count)
        {
            _logger.LogDebug("📋 {Symbol}: {New}/{Total} patterns added ({Skipped} duplicates skipped)",
                item.Symbol, newPatternsAdded, allPatterns.Count, allPatterns.Count - newPatternsAdded);
        }

        // Build notification
        var topPattern = allPatterns.OrderByDescending(a => a.Pattern.Confidence).Select(a => a.Pattern).FirstOrDefault();
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
                    var topPatternTf = allPatterns
                        .Where(a => a.Pattern == topPattern)
                        .Select(a => a.Timeframe.ToString())
                        .FirstOrDefault() ?? "Daily";

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
                        PatternTimeframe = topPatternTf,
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
                        PatternTimeframe = topPatternTf,
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

        var latestPrice = allPatterns.LastOrDefault().LastClose;

        var notification = new ScanNotification
        {
            Symbol = item.Symbol,
            PatternCount = allPatterns.Count,
            CurrentPrice = latestPrice,
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
            "Scanned {Symbol}: {Count} patterns (1H/4H/D/W), bias={Bias}, AI={AI}",
            item.Symbol, allPatterns.Count, aiSynthesis?.OverallBias ?? "N/A", useAI ? "yes" : "skipped");
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
    /// Update lifecycle of all live patterns — check prices, transition statuses, expire old ones.
    /// Runs every ~5 minutes.
    /// </summary>
    private async Task UpdatePatternLifecyclesAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var lifecycle = scope.ServiceProvider.GetRequiredService<PatternLifecycleService>();

            // Backfill expiry dates for patterns that predate the lifecycle system
            await lifecycle.BackfillExpiriesAsync(ct);

            // Update all live patterns
            await lifecycle.UpdateAllLivePatternsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error updating pattern lifecycles");
        }
    }

    /// <summary>
    /// Auto-run the Portfolio Advisor for all users with watchlist items.
    /// Saves advice history so it's ready when users open the app.
    /// Runs every N hours (default 6).
    /// </summary>
    private async Task AutoRunPortfolioAdvisorAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var advisor = scope.ServiceProvider.GetRequiredService<IAIAdvisor>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

            // Find users that have active watchlist items
            var userIds = await context.Set<WatchlistItem>()
                .Where(w => w.IsActive)
                .Select(w => w.UserId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var userId in userIds)
            {
                try
                {
                    var user = await context.Users
                        .OfType<Domain.Entities.Identity.ApplicationUser>()
                        .FirstOrDefaultAsync(u => u.Id == userId, ct);
                    if (user is null) continue;

                    var watchlist = await context.Set<WatchlistItem>()
                        .Where(w => w.UserId == userId && w.IsActive)
                        .ToListAsync(ct);
                    if (!watchlist.Any()) continue;

                    var openPositions = await context.Positions
                        .Where(p => p.UserId == userId && p.Status == PositionStatus.Open && p.IsActive)
                        .ToListAsync(ct);

                    var totalInvested = openPositions.Sum(p => p.EntryPrice * p.Quantity);
                    var realizedPnL = 0m;
                    try
                    {
                        realizedPnL = await context.SimulatedTrades
                            .Where(t => t.UserId == userId && t.Status == SimulationStatus.Resolved)
                            .SumAsync(t => t.PnLDollars ?? 0, ct);
                    }
                    catch { }
                    var cashAvailable = user.StartingCapital + realizedPnL - totalInvested;

                    // Only reference LIVE patterns (Active or PlayingOut)
                    var livePatterns = await context.DetectedPatterns
                        .Where(p => p.UserId == userId
                            && (p.Status == PatternStatus.Active || p.Status == PatternStatus.PlayingOut)
                            && watchlist.Select(w => w.Symbol).Contains(p.Asset))
                        .OrderByDescending(p => p.Confidence)
                        .Take(20)
                        .ToListAsync(ct);

                    // Build concise prompt
                    var prompt = new System.Text.StringBuilder();
                    prompt.AppendLine("Respond with ONLY valid JSON. No markdown, no text outside JSON.");
                    prompt.AppendLine($"Cash: ${cashAvailable:N0}. Max per position: 25%. Open positions: {openPositions.Count}.");

                    if (openPositions.Any())
                    {
                        prompt.Append("Current: ");
                        prompt.AppendLine(string.Join(", ", openPositions.Select(p => $"{p.Symbol}(${p.EntryPrice * p.Quantity:N0})")));
                    }

                    prompt.AppendLine("Evaluate (only LIVE patterns):");
                    foreach (var w in watchlist)
                    {
                        var patterns = livePatterns.Where(p => p.Asset == w.Symbol).ToList();
                        var hasPos = openPositions.Any(p => p.Symbol == w.Symbol);
                        prompt.Append($"- {w.Symbol}: {patterns.Count} live patterns");
                        if (patterns.Any())
                        {
                            var best = patterns.First();
                            prompt.Append($", best={best.PatternType}({best.Direction}), confidence={best.Confidence:F0}%, status={best.Status}");
                        }
                        prompt.Append($", bias={w.LastBias ?? "N/A"}");
                        if (hasPos) prompt.Append(", HAS_POSITION");
                        prompt.AppendLine();
                    }

                    prompt.AppendLine(@"JSON: {""summary"":""1 sentence"",""totalSuggestedAllocation"":0,""cashRetained"":0,""allocations"":[{""symbol"":""X"",""suggestedBudget"":0,""direction"":""Long"",""confidence"":""High"",""rationale"":""why"",""riskNote"":""risk"",""portfolioPercent"":0,""skip"":false,""skipReason"":null}],""warnings"":[],""diversificationScore"":""Good""}");

                    var response = await advisor.GetAdvisoryAsync(prompt.ToString());

                    // Parse and save
                    var jsonStart = response.IndexOf('{');
                    var jsonEnd = response.LastIndexOf('}');
                    if (jsonStart >= 0 && jsonEnd > jsonStart)
                    {
                        var json = response[jsonStart..(jsonEnd + 1)];
                        using var doc = System.Text.Json.JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var advice = new Domain.Entities.AI.PortfolioAdvice
                        {
                            CashAvailable = cashAvailable,
                            TotalInvested = totalInvested,
                            OpenPositionCount = openPositions.Count,
                            WatchlistCount = watchlist.Count,
                            Summary = root.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "",
                            DiversificationScore = root.TryGetProperty("diversificationScore", out var d) ? d.GetString() ?? "" : "",
                            TotalSuggestedAllocation = root.TryGetProperty("totalSuggestedAllocation", out var ta) ? ta.GetDecimal() : 0,
                            CashRetained = root.TryGetProperty("cashRetained", out var cr) ? cr.GetDecimal() : 0,
                            ResponseJson = json,
                            TotalAllocations = root.TryGetProperty("allocations", out var allocs) ? allocs.GetArrayLength() : 0,
                            UserId = userId
                        };

                        context.PortfolioAdvices.Add(advice);
                        await context.SaveChangesAsync(ct);

                        _logger.LogInformation("🧠 Auto-ran Portfolio Advisor for user {UserId}: {Allocations} allocations, ${Total:N0} suggested",
                            userId, advice.TotalAllocations, advice.TotalSuggestedAllocation);

                        // ═══════════════════════════════════════════════════════════════
                        // AUTO-EXECUTE: Create positions from advisor allocations
                        // Only if user has AI trading budget > 0%
                        // ═══════════════════════════════════════════════════════════════
                        if (user.AiTradingBudgetPercent > 0 && allocs.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var marketData = scope.ServiceProvider
                                .GetRequiredService<Application.Common.Interfaces.Market.IMarketDataService>();

                            var aiBudgetTotal = cashAvailable * user.AiTradingBudgetPercent / 100m;
                            var aiInvested = await context.Positions
                                .Where(p => p.UserId == userId && p.Status == PositionStatus.Open && p.IsActive && p.IsAiGenerated)
                                .SumAsync(p => p.EntryPrice * p.Quantity, ct);
                            var aiCashRemaining = aiBudgetTotal - aiInvested;

                            var positionsCreated = 0;

                            foreach (var allocEl in allocs.EnumerateArray())
                            {
                                if (aiCashRemaining <= 0) break;

                                try
                                {
                                    // Parse allocation
                                    var skip = allocEl.TryGetProperty("skip", out var skipProp) && skipProp.GetBoolean();
                                    if (skip) continue;

                                    var symbol = allocEl.TryGetProperty("symbol", out var symProp) ? symProp.GetString() ?? "" : "";
                                    var directionStr = allocEl.TryGetProperty("direction", out var dirProp) ? dirProp.GetString() ?? "Long" : "Long";
                                    var suggestedBudget = allocEl.TryGetProperty("suggestedBudget", out var budProp) ? budProp.GetDecimal() : 0;

                                    if (string.IsNullOrEmpty(symbol) || suggestedBudget <= 0) continue;

                                    // Cap allocation by remaining AI budget
                                    var positionBudget = Math.Min(suggestedBudget, aiCashRemaining);
                                    if (positionBudget < 10) continue;

                                    // Check no existing open AI position for this symbol
                                    var hasExisting = await context.Positions
                                        .AnyAsync(p => p.UserId == userId && p.Symbol == symbol
                                            && p.Status == PositionStatus.Open && p.IsActive && p.IsAiGenerated, ct);
                                    if (hasExisting)
                                    {
                                        _logger.LogDebug("AI advisor: already has position for {Symbol} — skipping", symbol);
                                        continue;
                                    }

                                    // Fetch current price
                                    decimal entryPrice;
                                    try
                                    {
                                        var candles = await marketData.GetCandlesAsync(symbol, 1, "1H");
                                        if (candles.Count == 0) continue;
                                        entryPrice = candles.Last().Close;
                                    }
                                    catch
                                    {
                                        _logger.LogDebug("AI advisor: failed to get price for {Symbol}", symbol);
                                        continue;
                                    }

                                    if (entryPrice <= 0) continue;

                                    // Determine asset class and calculate quantity
                                    var isCrypto = symbol.EndsWith("USD", StringComparison.OrdinalIgnoreCase)
                                        && !symbol.Contains(".") && symbol.Length <= 10;
                                    var quantity = isCrypto
                                        ? Math.Round(positionBudget / entryPrice, 6)
                                        : Math.Floor(positionBudget / entryPrice);
                                    if (quantity <= 0) continue;

                                    var direction = directionStr.Contains("Short", StringComparison.OrdinalIgnoreCase)
                                        ? SignalType.Short : SignalType.Long;

                                    var confidence = allocEl.TryGetProperty("confidence", out var confProp)
                                        ? confProp.GetString() ?? "Medium" : "Medium";
                                    var rationale = allocEl.TryGetProperty("rationale", out var ratProp)
                                        ? ratProp.GetString() ?? "" : "";

                                    var position = new Position
                                    {
                                        Symbol = symbol,
                                        AssetClass = isCrypto ? AssetClass.Crypto : AssetClass.Stock,
                                        SignalType = direction,
                                        EntryPrice = entryPrice,
                                        Quantity = quantity,
                                        CurrentPrice = entryPrice,
                                        Status = PositionStatus.Open,
                                        IsAiGenerated = true,
                                        Notes = $"AI Advisor allocation: {direction} {symbol} ${positionBudget:N0} ({confidence} confidence). {rationale}",
                                        UserId = userId
                                    };

                                    context.Positions.Add(position);
                                    aiCashRemaining -= quantity * entryPrice;
                                    positionsCreated++;

                                    _logger.LogInformation(
                                        "🤖 AI Advisor executed: {Symbol} {Direction} {Qty} units @ ${Entry:N2} (budget: ${Budget:N0})",
                                        symbol, direction, quantity, entryPrice, positionBudget);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "Failed to execute advisor allocation");
                                }
                            }

                            if (positionsCreated > 0)
                            {
                                await context.SaveChangesAsync(ct);
                                _logger.LogInformation("🤖 AI Advisor auto-executed {Count} positions for user {UserId}",
                                    positionsCreated, userId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to auto-run Portfolio Advisor for user {UserId}", userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error in auto Portfolio Advisor run");
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