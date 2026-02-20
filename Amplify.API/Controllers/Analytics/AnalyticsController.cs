using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Amplify.API.Controllers.Analytics;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public AnalyticsController(ApplicationDbContext context)
        => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetAnalytics()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // All signals
        var signals = await _context.TradeSignals
            .Where(s => s.UserId == userId)
            .ToListAsync();

        // All overrides
        var overrides = await _context.UserOverrides
            .Include(o => o.TradeSignal)
            .Where(o => o.UserId == userId)
            .ToListAsync();

        // Signal stats
        var totalSignals = signals.Count;
        var activeSignals = signals.Count(s => s.IsActive);
        var avgScore = signals.Any() ? Math.Round(signals.Average(s => (double)s.SetupScore), 1) : 0;
        var highConviction = signals.Count(s => s.SetupScore >= 75);
        var lowConviction = signals.Count(s => s.SetupScore < 50);

        // Signal type distribution
        var signalTypeDistribution = signals
            .GroupBy(s => s.SignalType.ToString())
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Asset distribution
        var assetDistribution = signals
            .GroupBy(s => s.Asset)
            .Select(g => new { Asset = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Regime distribution
        var regimeDistribution = signals
            .GroupBy(s => s.Regime.ToString())
            .Select(g => new { Regime = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Override stats
        var totalOverrides = overrides.Count;
        var accepted = overrides.Count(o => o.OverrideType == Domain.Enumerations.OverrideType.Accepted);
        var rejected = overrides.Count(o => o.OverrideType == Domain.Enumerations.OverrideType.Rejected);
        var modified = overrides.Count(o => o.OverrideType == Domain.Enumerations.OverrideType.Modified);

        // Override reason breakdown
        var reasonBreakdown = overrides
            .GroupBy(o => o.Reason.ToString())
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Outcome tracking
        var withOutcomes = overrides.Where(o => o.WasCorrect.HasValue).ToList();
        var correctDecisions = withOutcomes.Count(o => o.WasCorrect == true);
        var incorrectDecisions = withOutcomes.Count(o => o.WasCorrect == false);
        var totalPnL = withOutcomes.Sum(o => o.ActualPnL ?? 0);
        var winRate = withOutcomes.Any() ? Math.Round(correctDecisions * 100.0 / withOutcomes.Count, 1) : 0;

        // Score distribution (buckets: 0-25, 25-50, 50-75, 75-100)
        var scoreDistribution = new[]
        {
            new { Bucket = "0-25", Count = signals.Count(s => s.SetupScore < 25) },
            new { Bucket = "25-50", Count = signals.Count(s => s.SetupScore >= 25 && s.SetupScore < 50) },
            new { Bucket = "50-75", Count = signals.Count(s => s.SetupScore >= 50 && s.SetupScore < 75) },
            new { Bucket = "75-100", Count = signals.Count(s => s.SetupScore >= 75) }
        };

        // Risk distribution
        var avgRisk = signals.Any() ? Math.Round(signals.Average(s => (double)s.RiskPercent), 1) : 0;
        var maxRisk = signals.Any() ? signals.Max(s => s.RiskPercent) : 0;
        var minRisk = signals.Any() ? signals.Min(s => s.RiskPercent) : 0;

        // Signals over time (by date)
        var signalsByDate = signals
            .GroupBy(s => s.CreatedAt.Date)
            .Select(g => new { Date = g.Key.ToString("MMM dd"), Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(new
        {
            // Signal stats
            TotalSignals = totalSignals,
            ActiveSignals = activeSignals,
            AvgScore = avgScore,
            HighConviction = highConviction,
            LowConviction = lowConviction,
            SignalTypeDistribution = signalTypeDistribution,
            AssetDistribution = assetDistribution,
            RegimeDistribution = regimeDistribution,
            ScoreDistribution = scoreDistribution,
            SignalsByDate = signalsByDate,

            // Risk stats
            AvgRisk = avgRisk,
            MaxRisk = maxRisk,
            MinRisk = minRisk,

            // Override stats
            TotalOverrides = totalOverrides,
            Accepted = accepted,
            Rejected = rejected,
            Modified = modified,
            ReasonBreakdown = reasonBreakdown,

            // Outcome stats
            CorrectDecisions = correctDecisions,
            IncorrectDecisions = incorrectDecisions,
            WinRate = winRate,
            TotalPnL = totalPnL
        });
    }
}