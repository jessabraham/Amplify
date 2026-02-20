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

    public DashboardController(ApplicationDbContext context)
        => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
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
            RecentSignals = recentSignals
        });
    }
}