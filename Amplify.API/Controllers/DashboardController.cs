using Amplify.Application.Common.Interfaces.Market;
using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Amplify.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IMarketDataService _marketData;

    public DashboardController(ApplicationDbContext context, IMarketDataService marketData)
    {
        _context = context;
        _marketData = marketData;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var signals = await _context.TradeSignals
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            var totalSignals = signals.Count;
            var longCount = signals.Count(s => s.SignalType == Domain.Enumerations.SignalType.Long);
            var shortCount = signals.Count(s => s.SignalType == Domain.Enumerations.SignalType.Short);
            var avgScore = signals.Any() ? signals.Average(s => s.SetupScore) : 0;
            var avgRisk = signals.Any() ? signals.Average(s => s.RiskPercent) : 0;
            var highConviction = signals.Count(s => s.SetupScore >= 75);

            // Regime breakdown
            var regimeBreakdown = signals
                .GroupBy(s => s.Regime.ToString())
                .Select(g => new { Regime = g.Key, Count = g.Count() })
                .ToList();

            // Asset breakdown
            var assetBreakdown = signals
                .GroupBy(s => s.Asset)
                .Select(g => new { Asset = g.Key, Count = g.Count() })
                .ToList();

            // Recent signals (last 5)
            var recentSignals = signals
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new
                {
                    s.Asset,
                    SignalType = s.SignalType.ToString(),
                    s.SetupScore,
                    s.EntryPrice,
                    s.CreatedAt
                })
                .ToList();

            // ── Portfolio summary ────────────────────────────────────────
            var totalInvested = 0m;
            var totalUnrealizedPnL = 0m;
            var openPositionCount = 0;
            var portfolioPositions = new List<object>();
            var realizedPnL = 0m;

            // Get user's starting capital
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            var startingCapital = user?.StartingCapital ?? 100_000m;

            // Realized P&L from resolved simulated trades
            try
            {
                realizedPnL = await _context.SimulatedTrades
                    .Where(t => t.UserId == userId && t.Status == Domain.Enumerations.SimulationStatus.Resolved)
                    .SumAsync(t => t.PnLDollars ?? 0);
            }
            catch { }

            try
            {
                var openPositions = await _context.Positions
                    .Where(p => p.UserId == userId && p.Status == Domain.Enumerations.PositionStatus.Open && p.IsActive)
                    .ToListAsync();

                // Refresh current prices from market data
                if (openPositions.Any())
                {
                    var symbols = openPositions.Select(p => p.Symbol).Distinct();
                    foreach (var sym in symbols)
                    {
                        try
                        {
                            var candles = await _marketData.GetCandlesAsync(sym, 1, "1H");
                            if (candles.Count > 0)
                            {
                                var latestPrice = candles.Last().Close;
                                foreach (var pos in openPositions.Where(p => p.Symbol == sym))
                                {
                                    pos.CurrentPrice = latestPrice;
                                    var dir = pos.SignalType == SignalType.Short ? -1 : 1;
                                    pos.UnrealizedPnL = dir * (latestPrice - pos.EntryPrice) * pos.Quantity;
                                    if (pos.EntryPrice > 0)
                                        pos.ReturnPercent = Math.Round((latestPrice - pos.EntryPrice) / pos.EntryPrice * 100m * dir, 2);
                                }
                            }
                        }
                        catch { /* price refresh is best-effort */ }
                    }
                    await _context.SaveChangesAsync();
                }

                totalInvested = openPositions.Sum(p => p.EntryPrice * p.Quantity);
                totalUnrealizedPnL = openPositions.Sum(p => p.UnrealizedPnL);
                openPositionCount = openPositions.Count;

                portfolioPositions = openPositions
                    .OrderByDescending(p => Math.Abs(p.UnrealizedPnL))
                    .Take(5)
                    .Select(p => (object)new
                    {
                        p.Symbol,
                        SignalType = p.SignalType.ToString(),
                        p.Quantity,
                        p.EntryPrice,
                        p.CurrentPrice,
                        p.UnrealizedPnL,
                        p.ReturnPercent,
                        p.StopLoss,
                        p.Target1,
                        p.EntryDateUtc
                    })
                    .ToList();
            }
            catch { /* Positions table may not exist yet */ }

            // ── Recent pattern detections ────────────────────────────────
            var recentPatterns = new List<object>();
            try
            {
                var rawPatterns = await _context.DetectedPatterns
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();

                recentPatterns = rawPatterns
                    .Select(p => (object)new
                    {
                        p.Asset,
                        PatternType = p.PatternType.ToString(),
                        Direction = p.Direction.ToString(),
                        p.Confidence,
                        p.AIAnalysis,
                        p.DetectedAtPrice,
                        p.SuggestedEntry,
                        p.CreatedAt
                    })
                    .ToList();
            }
            catch { /* DetectedPatterns table may not exist yet */ }

            // ── Recent regime detections ─────────────────────────────────
            var recentRegimes = new List<object>();
            try
            {
                var rawRegimes = await _context.RegimeHistory
                    .OrderByDescending(r => r.DetectedAt)
                    .Take(5)
                    .ToListAsync();

                recentRegimes = rawRegimes
                    .Select(r => (object)new
                    {
                        r.Symbol,
                        Regime = r.Regime.ToString(),
                        r.Confidence,
                        r.DetectedAt
                    })
                    .ToList();
            }
            catch { /* RegimeHistory table may not exist yet */ }

            return Ok(new
            {
                TotalSignals = totalSignals,
                LongCount = longCount,
                ShortCount = shortCount,
                AvgScore = Math.Round(avgScore, 1),
                AvgRisk = Math.Round(avgRisk, 1),
                HighConviction = highConviction,
                RegimeBreakdown = regimeBreakdown,
                AssetBreakdown = assetBreakdown,
                RecentSignals = recentSignals,
                // New fields
                TotalInvested = Math.Round(totalInvested, 2),
                TotalUnrealizedPnL = Math.Round(totalUnrealizedPnL, 2),
                OpenPositionCount = openPositionCount,
                StartingCapital = startingCapital,
                CashAvailable = Math.Round(Math.Max(startingCapital + realizedPnL - totalInvested, 0), 2),
                RealizedPnL = Math.Round(realizedPnL, 2),
                PortfolioValue = Math.Round(startingCapital + realizedPnL + totalUnrealizedPnL, 2),
                TopPositions = portfolioPositions,
                RecentPatterns = recentPatterns,
                RecentRegimes = recentRegimes
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}