using Amplify.Application.Common.Interfaces.Market;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Amplify.Infrastructure.Services;

/// <summary>
/// Manages the lifecycle of detected patterns:
/// - Updates prices and high/low water marks
/// - Transitions: Active → PlayingOut → HitTarget/HitStop/Expired
/// - Invalidates contradictory patterns
/// </summary>
public class PatternLifecycleService
{
    private readonly ApplicationDbContext _context;
    private readonly IMarketDataService _marketData;
    private readonly ILogger<PatternLifecycleService> _logger;

    public PatternLifecycleService(
        ApplicationDbContext context,
        IMarketDataService marketData,
        ILogger<PatternLifecycleService> logger)
    {
        _context = context;
        _marketData = marketData;
        _logger = logger;
    }

    /// <summary>
    /// Check all live patterns and update their status based on current prices.
    /// Called by the background scanner on a regular interval.
    /// </summary>
    public async Task UpdateAllLivePatternsAsync(CancellationToken ct = default)
    {
        var livePatterns = await _context.DetectedPatterns
            .Where(p => p.Status == PatternStatus.Active || p.Status == PatternStatus.PlayingOut)
            .ToListAsync(ct);

        if (!livePatterns.Any()) return;

        // Group by symbol to minimize API calls
        var bySymbol = livePatterns.GroupBy(p => p.Asset).ToList();
        var priceCache = new Dictionary<string, decimal>();

        foreach (var group in bySymbol)
        {
            var symbol = group.Key;
            try
            {
                var candles = await _marketData.GetCandlesAsync(symbol, 1, "1H");
                if (candles.Any())
                {
                    priceCache[symbol] = candles.Last().Close;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get price for {Symbol}", symbol);
            }
        }

        var resolved = 0;
        var expired = 0;
        var playingOut = 0;

        foreach (var pattern in livePatterns)
        {
            if (!priceCache.TryGetValue(pattern.Asset, out var currentPrice))
                continue;

            pattern.CurrentPrice = currentPrice;
            pattern.UpdatedAt = DateTime.UtcNow;

            // Update water marks
            pattern.HighWaterMark = pattern.HighWaterMark.HasValue
                ? Math.Max(pattern.HighWaterMark.Value, currentPrice)
                : currentPrice;
            pattern.LowWaterMark = pattern.LowWaterMark.HasValue
                ? Math.Min(pattern.LowWaterMark.Value, currentPrice)
                : currentPrice;

            // Check expiry first
            if (DateTime.UtcNow >= pattern.ExpiresAt)
            {
                pattern.Status = PatternStatus.Expired;
                pattern.ResolvedAt = DateTime.UtcNow;
                pattern.ResolutionPrice = currentPrice;
                pattern.WasCorrect = EvaluateOutcome(pattern, currentPrice);
                pattern.ActualPnLPercent = CalculatePnLPercent(pattern, currentPrice);
                expired++;
                continue;
            }

            // Check if target or stop was hit
            if (pattern.Direction == PatternDirection.Bullish)
            {
                if (currentPrice >= pattern.SuggestedTarget && pattern.SuggestedTarget > 0)
                {
                    pattern.Status = PatternStatus.HitTarget;
                    pattern.ResolvedAt = DateTime.UtcNow;
                    pattern.ResolutionPrice = currentPrice;
                    pattern.WasCorrect = true;
                    pattern.ActualPnLPercent = CalculatePnLPercent(pattern, currentPrice);
                    resolved++;
                }
                else if (currentPrice <= pattern.SuggestedStop && pattern.SuggestedStop > 0)
                {
                    pattern.Status = PatternStatus.HitStop;
                    pattern.ResolvedAt = DateTime.UtcNow;
                    pattern.ResolutionPrice = currentPrice;
                    pattern.WasCorrect = false;
                    pattern.ActualPnLPercent = CalculatePnLPercent(pattern, currentPrice);
                    resolved++;
                }
                else if (currentPrice > pattern.DetectedAtPrice && pattern.Status == PatternStatus.Active)
                {
                    // Moving in the right direction
                    pattern.Status = PatternStatus.PlayingOut;
                    playingOut++;
                }
            }
            else if (pattern.Direction == PatternDirection.Bearish)
            {
                if (currentPrice <= pattern.SuggestedTarget && pattern.SuggestedTarget > 0)
                {
                    pattern.Status = PatternStatus.HitTarget;
                    pattern.ResolvedAt = DateTime.UtcNow;
                    pattern.ResolutionPrice = currentPrice;
                    pattern.WasCorrect = true;
                    pattern.ActualPnLPercent = CalculatePnLPercent(pattern, currentPrice);
                    resolved++;
                }
                else if (currentPrice >= pattern.SuggestedStop && pattern.SuggestedStop > 0)
                {
                    pattern.Status = PatternStatus.HitStop;
                    pattern.ResolvedAt = DateTime.UtcNow;
                    pattern.ResolutionPrice = currentPrice;
                    pattern.WasCorrect = false;
                    pattern.ActualPnLPercent = CalculatePnLPercent(pattern, currentPrice);
                    resolved++;
                }
                else if (currentPrice < pattern.DetectedAtPrice && pattern.Status == PatternStatus.Active)
                {
                    pattern.Status = PatternStatus.PlayingOut;
                    playingOut++;
                }
            }
        }

        await _context.SaveChangesAsync(ct);

        if (resolved > 0 || expired > 0 || playingOut > 0)
        {
            _logger.LogInformation(
                "📊 Pattern lifecycle: {Resolved} resolved, {Expired} expired, {PlayingOut} playing out (of {Total} live)",
                resolved, expired, playingOut, livePatterns.Count);
        }
    }

    /// <summary>
    /// Set ExpiresAt on patterns that don't have one yet (backfill for existing data).
    /// </summary>
    public async Task BackfillExpiriesAsync(CancellationToken ct = default)
    {
        var missing = await _context.DetectedPatterns
            .Where(p => p.ExpiresAt == default)
            .ToListAsync(ct);

        foreach (var p in missing)
        {
            p.ExpiresAt = DetectedPattern.CalculateExpiry(p.Timeframe, p.CreatedAt);
        }

        if (missing.Any())
        {
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("Backfilled expiry for {Count} patterns", missing.Count);
        }
    }

    private static bool EvaluateOutcome(DetectedPattern pattern, decimal finalPrice)
    {
        if (pattern.Direction == PatternDirection.Bullish)
            return finalPrice > pattern.DetectedAtPrice;
        if (pattern.Direction == PatternDirection.Bearish)
            return finalPrice < pattern.DetectedAtPrice;
        return false;
    }

    private static decimal CalculatePnLPercent(DetectedPattern pattern, decimal finalPrice)
    {
        if (pattern.DetectedAtPrice == 0) return 0;

        var pnl = pattern.Direction == PatternDirection.Bearish
            ? (pattern.DetectedAtPrice - finalPrice) / pattern.DetectedAtPrice * 100
            : (finalPrice - pattern.DetectedAtPrice) / pattern.DetectedAtPrice * 100;

        return Math.Round(pnl, 2);
    }
}