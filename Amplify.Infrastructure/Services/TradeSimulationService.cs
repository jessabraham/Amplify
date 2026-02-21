using Amplify.Application.Common.Interfaces.Market;
using Amplify.Application.Common.Models;
using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Amplify.Infrastructure.Services;

/// <summary>
/// Manages the full lifecycle of simulated trades:
/// 1. Creates a SimulatedTrade when a signal is saved
/// 2. Resolves open trades against new price data (real Alpaca data when available)
/// 3. Updates PatternPerformance aggregates after resolution
/// 4. Provides stats for the AI feedback loop
/// </summary>
public class TradeSimulationService
{
    private readonly ApplicationDbContext _context;
    private readonly IMarketDataService _marketData;
    private readonly ILogger<TradeSimulationService> _logger;

    public TradeSimulationService(ApplicationDbContext context, IMarketDataService marketData, ILogger<TradeSimulationService> logger)
    {
        _context = context;
        _marketData = marketData;
        _logger = logger;
    }

    public async Task<TradeSignal?> GetSignalAsync(Guid id)
        => await _context.TradeSignals.FindAsync(id);

    // ═══════════════════════════════════════════════════════════════════
    // 1. CREATE SIMULATED TRADE FROM SIGNAL
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called from the API controller with pattern/timeframe context.
    /// </summary>
    public async Task<SimulatedTrade> CreateFromSignalAsync(TradeSignal signal, dynamic? context = null)
    {
        var trade = new SimulatedTrade
        {
            TradeSignalId = signal.Id,
            Asset = signal.Asset,
            Direction = signal.SignalType,
            EntryPrice = signal.EntryPrice,
            StopLoss = signal.StopLoss,
            Target1 = signal.Target1,
            Target2 = signal.Target2 > 0 ? signal.Target2 : null,
            RegimeAtEntry = signal.Regime,
            UserId = signal.UserId,
            Status = SimulationStatus.Active,
            ActivatedAt = DateTime.UtcNow,
            HighestPriceSeen = signal.EntryPrice,
            LowestPriceSeen = signal.EntryPrice,
            AIConfidence = signal.AIConfidence,
            AIRecommendedAction = signal.AIRecommendedAction,
            ShareCount = signal.RiskShareCount,
            PositionValue = signal.RiskPositionValue,
            MaxRisk = signal.RiskMaxLoss
        };

        // Copy context from request if available
        if (context is not null)
        {
            try
            {
                string? ptStr = context.PatternType;
                string? pdStr = context.PatternDirection;
                if (!string.IsNullOrEmpty(ptStr) && Enum.TryParse<PatternType>(ptStr, out var pt))
                    trade.PatternType = pt;
                if (!string.IsNullOrEmpty(pdStr) && Enum.TryParse<PatternDirection>(pdStr, out var pd))
                    trade.PatternDirection = pd;
                trade.PatternTimeframe = (string?)context.PatternTimeframe;
                trade.PatternConfidence = (decimal?)context.PatternConfidence;
                trade.TimeframeAlignment = (string?)context.TimeframeAlignment;
                trade.RegimeAlignment = (string?)context.RegimeAlignment;
                trade.MAAlignment = (string?)context.MAAlignment;
                trade.VolumeProfile = (string?)context.VolumeProfile;
                trade.RSIAtEntry = (decimal?)context.RSIAtEntry;
            }
            catch { /* ignore context parse errors */ }
        }

        _context.SimulatedTrades.Add(trade);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created simulated trade {TradeId} for {Asset} {Direction} at {Entry}",
            trade.Id, trade.Asset, trade.Direction, trade.EntryPrice);

        return trade;
    }

    // ═══════════════════════════════════════════════════════════════════
    // 2. RESOLVE TRADES AGAINST PRICE DATA
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Called periodically (or on-demand) to check all active trades against
    /// current/simulated prices. Resolves trades that hit target or stop.
    /// </summary>
    public async Task<List<SimulatedTrade>> ResolveActiveTradesAsync(string? asset = null)
    {
        var query = _context.SimulatedTrades
            .Where(t => t.Status == SimulationStatus.Active)
            .AsQueryable();

        if (!string.IsNullOrEmpty(asset))
            query = query.Where(t => t.Asset == asset);

        var activeTrades = await query.ToListAsync();
        var resolved = new List<SimulatedTrade>();

        // Group trades by asset to minimize API calls
        var tradesByAsset = activeTrades.GroupBy(t => t.Asset);

        foreach (var group in tradesByAsset)
        {
            var symbol = group.Key;
            List<Candle> candles;

            try
            {
                // Fetch real price data from Alpaca (or fallback to sample)
                // Get enough bars to cover from the earliest trade entry
                var earliestEntry = group.Min(t => t.ActivatedAt ?? t.CreatedAt);
                var daysSince = (int)(DateTime.UtcNow - earliestEntry).TotalDays + 5;
                candles = await _marketData.GetCandlesAsync(symbol, Math.Max(daysSince, 30), "Daily");

                _logger.LogDebug("Fetched {Count} candles for {Symbol} from {Source} to resolve trades",
                    candles.Count, symbol, _marketData.DataSource);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch candles for {Symbol} — skipping resolution", symbol);
                continue;
            }

            foreach (var trade in group)
            {
                // Get only candles AFTER the trade was activated
                var entryDate = trade.ActivatedAt ?? trade.CreatedAt;
                var relevantCandles = candles
                    .Where(c => c.Time > entryDate.Date)
                    .OrderBy(c => c.Time)
                    .Skip(trade.DaysHeld) // Skip already-processed days
                    .ToList();

                if (!relevantCandles.Any()) continue;

                var wasResolved = ProcessTradeAgainstPrices(trade, relevantCandles);
                if (wasResolved)
                {
                    resolved.Add(trade);
                    _logger.LogInformation("Resolved trade {TradeId}: {Asset} {Outcome} P&L: {PnL:F2}% (real data from {Source})",
                        trade.Id, trade.Asset, trade.Outcome, trade.PnLPercent, _marketData.DataSource);
                }
            }
        }

        if (resolved.Any())
        {
            await _context.SaveChangesAsync();

            // Update performance aggregates for each resolved trade
            foreach (var trade in resolved)
                await UpdatePatternPerformanceAsync(trade);

            // Also update the DetectedPattern outcome if linked
            await UpdateDetectedPatternOutcomesAsync(resolved);
        }

        return resolved;
    }

    /// <summary>
    /// Process a single trade against a series of price bars.
    /// Returns true if the trade was resolved.
    /// </summary>
    private bool ProcessTradeAgainstPrices(SimulatedTrade trade, List<Candle> candles)
    {
        var isLong = trade.Direction == SignalType.Long;

        foreach (var candle in candles)
        {
            trade.DaysHeld++;

            // Track extremes
            if (candle.High > (trade.HighestPriceSeen ?? 0))
                trade.HighestPriceSeen = candle.High;
            if (candle.Low < (trade.LowestPriceSeen ?? decimal.MaxValue))
                trade.LowestPriceSeen = candle.Low;

            // Check stop loss hit
            bool stopHit = isLong
                ? candle.Low <= trade.StopLoss
                : candle.High >= trade.StopLoss;

            // Check target hit
            bool targetHit = isLong
                ? candle.High >= trade.Target1
                : candle.Low <= trade.Target1;

            if (stopHit && targetHit)
            {
                // Both hit same candle — use open direction to determine which first
                // Simplified: if open is closer to stop, stop hit first
                if (isLong)
                    stopHit = candle.Open <= (trade.EntryPrice + trade.StopLoss) / 2;
                else
                    stopHit = candle.Open >= (trade.EntryPrice + trade.StopLoss) / 2;
                targetHit = !stopHit;
            }

            if (stopHit)
            {
                ResolveTrade(trade, TradeOutcome.HitStop, trade.StopLoss);
                return true;
            }

            if (targetHit)
            {
                ResolveTrade(trade, TradeOutcome.HitTarget1, trade.Target1);
                return true;
            }
        }

        // Check expiration
        if (trade.DaysHeld >= trade.MaxExpirationDays)
        {
            var lastPrice = candles.Any() ? candles.Last().Close : trade.EntryPrice;
            ResolveTrade(trade, TradeOutcome.Expired, lastPrice);
            return true;
        }

        return false;
    }

    private void ResolveTrade(SimulatedTrade trade, TradeOutcome outcome, decimal exitPrice)
    {
        var isLong = trade.Direction == SignalType.Long;
        trade.Outcome = outcome;
        trade.Status = SimulationStatus.Resolved;
        trade.ResolvedAt = DateTime.UtcNow;
        trade.ExitPrice = exitPrice;

        // Calculate P&L
        trade.PnLPercent = isLong
            ? (exitPrice - trade.EntryPrice) / trade.EntryPrice * 100
            : (trade.EntryPrice - exitPrice) / trade.EntryPrice * 100;

        if (trade.ShareCount.HasValue)
        {
            trade.PnLDollars = isLong
                ? (exitPrice - trade.EntryPrice) * trade.ShareCount.Value
                : (trade.EntryPrice - exitPrice) * trade.ShareCount.Value;
        }

        // R-Multiple: how many R's did we make/lose
        var riskPerShare = Math.Abs(trade.EntryPrice - trade.StopLoss);
        if (riskPerShare > 0)
        {
            var pnlPerShare = isLong ? exitPrice - trade.EntryPrice : trade.EntryPrice - exitPrice;
            trade.RMultiple = pnlPerShare / riskPerShare;
        }

        // Max drawdown
        if (isLong && trade.LowestPriceSeen.HasValue)
            trade.MaxDrawdownPercent = (trade.EntryPrice - trade.LowestPriceSeen.Value) / trade.EntryPrice * 100;
        else if (!isLong && trade.HighestPriceSeen.HasValue)
            trade.MaxDrawdownPercent = (trade.HighestPriceSeen.Value - trade.EntryPrice) / trade.EntryPrice * 100;
    }

    // ═══════════════════════════════════════════════════════════════════
    // 3. UPDATE PATTERN PERFORMANCE AGGREGATES
    // ═══════════════════════════════════════════════════════════════════

    private async Task UpdatePatternPerformanceAsync(SimulatedTrade trade)
    {
        if (!trade.PatternType.HasValue || !trade.PatternDirection.HasValue) return;

        var timeframe = trade.PatternTimeframe ?? "Daily";
        var regime = trade.RegimeAtEntry;

        // Find or create the performance record
        var perf = await _context.PatternPerformances.FirstOrDefaultAsync(p =>
            p.PatternType == trade.PatternType.Value &&
            p.Direction == trade.PatternDirection.Value &&
            p.Timeframe == timeframe &&
            p.Regime == regime &&
            p.UserId == trade.UserId);

        if (perf is null)
        {
            perf = new PatternPerformance
            {
                PatternType = trade.PatternType.Value,
                Direction = trade.PatternDirection.Value,
                Timeframe = timeframe,
                Regime = regime,
                UserId = trade.UserId
            };
            _context.PatternPerformances.Add(perf);
        }

        // Update counts
        perf.TotalTrades++;
        var isWin = trade.Outcome == TradeOutcome.HitTarget1 || trade.Outcome == TradeOutcome.HitTarget2;
        var isLoss = trade.Outcome == TradeOutcome.HitStop;

        if (isWin) perf.Wins++;
        else if (isLoss) perf.Losses++;
        else perf.Expired++;

        var totalDecided = perf.Wins + perf.Losses;
        perf.WinRate = totalDecided > 0 ? (decimal)perf.Wins / totalDecided * 100 : 0;

        // Update P&L stats from all resolved trades for this combo
        var allTrades = await _context.SimulatedTrades
            .Where(t => t.PatternType == trade.PatternType.Value &&
                        t.PatternDirection == trade.PatternDirection.Value &&
                        t.PatternTimeframe == timeframe &&
                        t.RegimeAtEntry == regime &&
                        t.UserId == trade.UserId &&
                        t.Status == SimulationStatus.Resolved)
            .ToListAsync();

        var wins = allTrades.Where(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitTarget2).ToList();
        var losses = allTrades.Where(t => t.Outcome == TradeOutcome.HitStop).ToList();

        perf.AvgWinPercent = wins.Any() ? wins.Average(t => t.PnLPercent ?? 0) : 0;
        perf.AvgLossPercent = losses.Any() ? losses.Average(t => t.PnLPercent ?? 0) : 0;
        perf.AvgRMultiple = allTrades.Any() ? allTrades.Average(t => t.RMultiple ?? 0) : 0;
        perf.BestTradePercent = allTrades.Any() ? allTrades.Max(t => t.PnLPercent ?? 0) : 0;
        perf.WorstTradePercent = allTrades.Any() ? allTrades.Min(t => t.PnLPercent ?? 0) : 0;
        perf.TotalPnLPercent = allTrades.Sum(t => t.PnLPercent ?? 0);
        perf.AvgDaysHeld = allTrades.Any() ? (decimal)allTrades.Average(t => t.DaysHeld) : 0;
        perf.LastTradeDate = DateTime.UtcNow;

        var grossWins = wins.Sum(t => Math.Abs(t.PnLPercent ?? 0));
        var grossLosses = losses.Sum(t => Math.Abs(t.PnLPercent ?? 0));
        perf.ProfitFactor = grossLosses > 0 ? grossWins / grossLosses : grossWins > 0 ? 99 : 0;

        // Context-specific win rates
        var aligned = allTrades.Where(t => t.TimeframeAlignment?.Contains("All") == true).ToList();
        var conflicting = allTrades.Where(t => t.TimeframeAlignment == "Conflicting").ToList();
        var breakoutVol = allTrades.Where(t => t.VolumeProfile == "Breakout").ToList();

        perf.TradesWhenAligned = aligned.Count;
        perf.WinRateWhenAligned = aligned.Any(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop)
            ? (decimal)aligned.Count(t => t.Outcome == TradeOutcome.HitTarget1) / aligned.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop) * 100
            : 0;

        perf.TradesWhenConflicting = conflicting.Count;
        perf.WinRateWhenConflicting = conflicting.Any(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop)
            ? (decimal)conflicting.Count(t => t.Outcome == TradeOutcome.HitTarget1) / conflicting.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop) * 100
            : 0;

        perf.TradesWithBreakoutVol = breakoutVol.Count;
        perf.WinRateWithBreakoutVol = breakoutVol.Any(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop)
            ? (decimal)breakoutVol.Count(t => t.Outcome == TradeOutcome.HitTarget1) / breakoutVol.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitStop) * 100
            : 0;

        perf.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Update the DetectedPattern entity with outcome data so scan history shows results.
    /// </summary>
    private async Task UpdateDetectedPatternOutcomesAsync(List<SimulatedTrade> resolved)
    {
        var signalIds = resolved.Where(t => t.TradeSignalId.HasValue).Select(t => t.TradeSignalId!.Value).ToList();
        var patterns = await _context.DetectedPatterns
            .Where(p => p.GeneratedSignalId.HasValue && signalIds.Contains(p.GeneratedSignalId.Value))
            .ToListAsync();

        foreach (var pattern in patterns)
        {
            var trade = resolved.FirstOrDefault(t => t.TradeSignalId == pattern.GeneratedSignalId);
            if (trade is null) continue;

            pattern.WasCorrect = trade.Outcome == TradeOutcome.HitTarget1 || trade.Outcome == TradeOutcome.HitTarget2;
            pattern.ActualPnLPercent = trade.PnLPercent;
            pattern.ResolvedAt = trade.ResolvedAt;
        }

        await _context.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════════════════════════════
    // 4. GET STATS FOR AI FEEDBACK LOOP
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get performance stats for patterns relevant to the current scan.
    /// The AI prompt builder calls this to include historical data.
    /// </summary>
    public async Task<List<PatternPerformance>> GetRelevantStatsAsync(
        string userId,
        List<PatternType> patternTypes,
        MarketRegime regime)
    {
        return await _context.PatternPerformances
            .Where(p => p.UserId == userId &&
                        patternTypes.Contains(p.PatternType) &&
                        p.TotalTrades >= 3) // Only include if enough data
            .OrderByDescending(p => p.TotalTrades)
            .ToListAsync();
    }

    /// <summary>
    /// Get user's overall trading stats for AI context.
    /// </summary>
    public async Task<UserTradingStats> GetUserStatsAsync(string userId)
    {
        var trades = await _context.SimulatedTrades
            .Where(t => t.UserId == userId && t.Status == SimulationStatus.Resolved)
            .ToListAsync();

        if (!trades.Any())
            return new UserTradingStats();

        var wins = trades.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitTarget2);
        var losses = trades.Count(t => t.Outcome == TradeOutcome.HitStop);
        var totalDecided = wins + losses;

        return new UserTradingStats
        {
            TotalTrades = trades.Count,
            Wins = wins,
            Losses = losses,
            WinRate = totalDecided > 0 ? (decimal)wins / totalDecided * 100 : 0,
            AvgRMultiple = trades.Average(t => t.RMultiple ?? 0),
            TotalPnLPercent = trades.Sum(t => t.PnLPercent ?? 0),
            BestTrade = trades.Max(t => t.PnLPercent ?? 0),
            WorstTrade = trades.Min(t => t.PnLPercent ?? 0),
            AvgDaysHeld = (decimal)trades.Average(t => t.DaysHeld),
            LongWinRate = CalculateWinRate(trades.Where(t => t.Direction == SignalType.Long)),
            ShortWinRate = CalculateWinRate(trades.Where(t => t.Direction == SignalType.Short)),
            AlignedWinRate = CalculateWinRate(trades.Where(t => t.TimeframeAlignment?.Contains("All") == true)),
            ConflictingWinRate = CalculateWinRate(trades.Where(t => t.TimeframeAlignment == "Conflicting"))
        };
    }

    /// <summary>
    /// Get active (open) simulated trades for the user.
    /// </summary>
    public async Task<List<SimulatedTrade>> GetActiveTradesAsync(string userId)
    {
        return await _context.SimulatedTrades
            .Where(t => t.UserId == userId && t.Status == SimulationStatus.Active)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get resolved trade history.
    /// </summary>
    public async Task<List<SimulatedTrade>> GetTradeHistoryAsync(string userId, int count = 50)
    {
        return await _context.SimulatedTrades
            .Where(t => t.UserId == userId && t.Status == SimulationStatus.Resolved)
            .OrderByDescending(t => t.ResolvedAt)
            .Take(count)
            .ToListAsync();
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════

    private static decimal CalculateWinRate(IEnumerable<SimulatedTrade> trades)
    {
        var list = trades.ToList();
        var wins = list.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitTarget2);
        var losses = list.Count(t => t.Outcome == TradeOutcome.HitStop);
        var total = wins + losses;
        return total > 0 ? (decimal)wins / total * 100 : 0;
    }

    /// <summary>
    /// Simulates price movement for an active trade.
    /// Replace with real Alpaca data when connected.
    /// </summary>
    // SimulatePriceMovement removed — now using real Alpaca market data via IMarketDataService

    // ═══════════════════════════════════════════════════════════════════
    // DELETE
    // ═══════════════════════════════════════════════════════════════════

    public async Task<bool> DeleteTradeAsync(Guid tradeId, string userId)
    {
        var trade = await _context.SimulatedTrades
            .FirstOrDefaultAsync(t => t.Id == tradeId && t.UserId == userId);
        if (trade is null) return false;

        _context.SimulatedTrades.Remove(trade);
        await _context.SaveChangesAsync();
        _logger.LogInformation("🗑️ Deleted simulated trade {TradeId} ({Asset})", tradeId, trade.Asset);
        return true;
    }

    public async Task<int> ClearAllTradesAsync(string userId)
    {
        var trades = await _context.SimulatedTrades
            .Where(t => t.UserId == userId)
            .ToListAsync();
        if (!trades.Any()) return 0;

        _context.SimulatedTrades.RemoveRange(trades);
        await _context.SaveChangesAsync();
        _logger.LogInformation("🗑️ Cleared {Count} simulated trades for user {UserId}", trades.Count, userId);
        return trades.Count;
    }
}

/// <summary>
/// User's overall trading statistics for AI context.
/// </summary>
public class UserTradingStats
{
    public int TotalTrades { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public decimal WinRate { get; set; }
    public decimal AvgRMultiple { get; set; }
    public decimal TotalPnLPercent { get; set; }
    public decimal BestTrade { get; set; }
    public decimal WorstTrade { get; set; }
    public decimal AvgDaysHeld { get; set; }
    public decimal LongWinRate { get; set; }
    public decimal ShortWinRate { get; set; }
    public decimal AlignedWinRate { get; set; }
    public decimal ConflictingWinRate { get; set; }
}