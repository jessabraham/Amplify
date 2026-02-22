using Amplify.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Amplify.API.Controllers.Trading;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SimulationController : ControllerBase
{
    private readonly TradeSimulationService _simulation;

    public SimulationController(TradeSimulationService simulation)
        => _simulation = simulation;

    /// <summary>
    /// Create a simulated trade from a saved signal.
    /// Called after signal creation in the Trade Planner.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> CreateSimulatedTrade([FromBody] CreateSimTradeRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var signal = await _simulation.GetSignalAsync(request.TradeSignalId);
        if (signal is null) return NotFound("Signal not found");
        if (signal.UserId != userId) return Forbid();

        var trade = await _simulation.CreateFromSignalAsync(signal, request);
        return Ok(new { trade.Id, trade.Asset, Direction = trade.Direction.ToString(), trade.Status });
    }

    /// <summary>
    /// Resolve all active simulated trades (run the simulation forward).
    /// Call this to check which trades have hit target/stop.
    /// </summary>
    [HttpPost("resolve")]
    public async Task<IActionResult> ResolveAll([FromQuery] string? asset = null)
    {
        var resolved = await _simulation.ResolveActiveTradesAsync(asset);
        return Ok(new
        {
            ResolvedCount = resolved.Count,
            Results = resolved.Select(t => new
            {
                t.Id,
                t.Asset,
                Direction = t.Direction.ToString(),
                Outcome = t.Outcome.ToString(),
                t.EntryPrice,
                t.ExitPrice,
                PnLPercent = t.PnLPercent?.ToString("F2"),
                PnLDollars = t.PnLDollars?.ToString("F2"),
                RMultiple = t.RMultiple?.ToString("F2"),
                t.DaysHeld,
                Pattern = t.PatternType?.ToString(),
                Timeframe = t.PatternTimeframe
            })
        });
    }

    /// <summary>
    /// Get all active (open) simulated trades.
    /// </summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveTrades()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var trades = await _simulation.GetActiveTradesAsync(userId);
        return Ok(trades.Select(t => new
        {
            t.Id,
            t.Asset,
            Direction = t.Direction.ToString(),
            t.EntryPrice,
            t.StopLoss,
            t.Target1,
            t.Target2,
            Regime = t.RegimeAtEntry.ToString(),
            Pattern = t.PatternType?.ToString(),
            Timeframe = t.PatternTimeframe,
            t.TimeframeAlignment,
            t.DaysHeld,
            t.MaxExpirationDays,
            t.HighestPriceSeen,
            t.LowestPriceSeen,
            Status = t.Status.ToString(),
            Source = t.TradeSignal?.Source.ToString() ?? "Manual",
            t.CreatedAt
        }));
    }

    /// <summary>
    /// Get resolved trade history with outcomes.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] int count = 50)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var trades = await _simulation.GetTradeHistoryAsync(userId, count);
        return Ok(trades.Select(t => new
        {
            t.Id,
            t.Asset,
            Direction = t.Direction.ToString(),
            Outcome = t.Outcome.ToString(),
            t.EntryPrice,
            t.ExitPrice,
            PnLPercent = t.PnLPercent,
            PnLDollars = t.PnLDollars,
            RMultiple = t.RMultiple,
            t.DaysHeld,
            Pattern = t.PatternType?.ToString(),
            PatternDirection = t.PatternDirection?.ToString(),
            Timeframe = t.PatternTimeframe,
            Regime = t.RegimeAtEntry.ToString(),
            t.TimeframeAlignment,
            t.VolumeProfile,
            t.MAAlignment,
            t.AIConfidence,
            t.AIRecommendedAction,
            t.PatternConfidence,
            MaxDrawdown = t.MaxDrawdownPercent,
            Source = t.TradeSignal?.Source.ToString() ?? "Manual",
            t.CreatedAt,
            t.ResolvedAt
        }));
    }

    /// <summary>
    /// Get user's overall trading stats.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetUserStats()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var stats = await _simulation.GetUserStatsAsync(userId);
        return Ok(stats);
    }

    /// <summary>
    /// Get pattern performance data (the learning feedback).
    /// </summary>
    [HttpGet("pattern-performance")]
    public async Task<IActionResult> GetPatternPerformance()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var perfs = await _simulation.GetRelevantStatsAsync(userId, new(), Domain.Enumerations.MarketRegime.Trending);

        // Get all instead of filtered
        var all = await _simulation.GetUserStatsAsync(userId);
        return Ok(new { UserStats = all, PatternPerformance = perfs });
    }
    /// <summary>
    /// Delete a simulated trade (active or resolved).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTrade(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var deleted = await _simulation.DeleteTradeAsync(id, userId);
        if (!deleted) return NotFound("Trade not found or not owned by you");
        return Ok(new { deleted = true, id });
    }

    /// <summary>
    /// Delete all simulated trades for the current user.
    /// </summary>
    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAll()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var count = await _simulation.ClearAllTradesAsync(userId);
        return Ok(new { deleted = count });
    }
}

public class CreateSimTradeRequest
{
    public Guid TradeSignalId { get; set; }
    public string? PatternType { get; set; }
    public string? PatternDirection { get; set; }
    public string? PatternTimeframe { get; set; }
    public decimal? PatternConfidence { get; set; }
    public string? TimeframeAlignment { get; set; }
    public string? RegimeAlignment { get; set; }
    public string? MAAlignment { get; set; }
    public string? VolumeProfile { get; set; }
    public decimal? RSIAtEntry { get; set; }
}