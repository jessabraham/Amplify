using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Amplify.API.Controllers.Trading;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BacktestController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BacktestController(ApplicationDbContext context)
        => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetBacktests()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var results = await _context.BacktestResults
            .Where(b => b.UserId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.Id,
                b.Asset,
                AssetClass = b.AssetClass.ToString(),
                SignalType = b.SignalType.ToString(),
                Regime = b.Regime.ToString(),
                b.StartDate,
                b.EndDate,
                b.InitialCapital,
                b.TotalTrades,
                b.WinRate,
                b.ProfitFactor,
                b.MaxDrawdown,
                b.NetPnL,
                b.SharpeRatio,
                b.CreatedAt
            })
            .ToListAsync();

        return Ok(results);
    }

    [HttpPost("run")]
    public async Task<IActionResult> RunBacktest([FromBody] RunBacktestRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Simulated backtest engine — generates realistic results
        var random = new Random();
        var totalTrades = random.Next(20, 150);
        var winRate = Math.Round(45 + random.NextDouble() * 30, 1); // 45-75%
        var wins = (int)(totalTrades * winRate / 100.0);
        var losses = totalTrades - wins;

        var avgWin = (decimal)(0.01 + random.NextDouble() * 0.03) * request.InitialCapital;
        var avgLoss = (decimal)(0.005 + random.NextDouble() * 0.02) * request.InitialCapital;
        var grossProfit = wins * avgWin;
        var grossLoss = losses * avgLoss;
        var netPnL = grossProfit - grossLoss;
        var profitFactor = grossLoss > 0 ? Math.Round(grossProfit / grossLoss, 2) : 0;
        var maxDrawdown = Math.Round(5 + random.NextDouble() * 20, 1); // 5-25%
        var sharpeRatio = Math.Round(0.5 + random.NextDouble() * 2.5, 2); // 0.5-3.0

        var result = new BacktestResult
        {
            UserId = userId,
            Asset = request.Asset,
            AssetClass = request.AssetClass,
            SignalType = request.SignalType,
            Regime = request.Regime,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            InitialCapital = request.InitialCapital,
            TotalTrades = totalTrades,
            WinRate = (decimal)winRate,
            ProfitFactor = (decimal)profitFactor,
            MaxDrawdown = (decimal)maxDrawdown,
            NetPnL = (decimal)Math.Round(netPnL, 2),
            SharpeRatio = (decimal)sharpeRatio
        };

        _context.BacktestResults.Add(result);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            result.Id,
            result.Asset,
            AssetClass = result.AssetClass.ToString(),
            SignalType = result.SignalType.ToString(),
            Regime = result.Regime.ToString(),
            result.StartDate,
            result.EndDate,
            result.InitialCapital,
            result.TotalTrades,
            result.WinRate,
            result.ProfitFactor,
            result.MaxDrawdown,
            result.NetPnL,
            result.SharpeRatio,
            result.CreatedAt
        });
    }
}

public class RunBacktestRequest
{
    public string Asset { get; set; } = "";
    public AssetClass AssetClass { get; set; }
    public SignalType SignalType { get; set; }
    public MarketRegime Regime { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
}