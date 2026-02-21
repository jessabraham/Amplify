using Amplify.Application.Common.Interfaces.AI;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Infrastructure.Persistence;
using Amplify.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace Amplify.API.Controllers.AI;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StrategyAdvisorController : ControllerBase
{
    private readonly IAIAdvisor _advisor;
    private readonly TradeSimulationService _simulation;
    private readonly ITradeSignalService _signalService;
    private readonly ApplicationDbContext _context;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public StrategyAdvisorController(
        IAIAdvisor advisor,
        TradeSimulationService simulation,
        ITradeSignalService signalService,
        ApplicationDbContext context)
    {
        _advisor = advisor;
        _simulation = simulation;
        _signalService = signalService;
        _context = context;
    }

    /// <summary>
    /// Generate a comprehensive AI strategy review based on the user's trading history.
    /// </summary>
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze([FromBody] StrategyAnalysisRequest request)
    {
        // ── Gather all performance data ──────────────────────────────
        var userStats = await _simulation.GetUserStatsAsync(UserId);
        var signalStatsResult = await _signalService.GetSignalStatsAsync(UserId);
        var signalStats = signalStatsResult.IsSuccess ? signalStatsResult.Value : null;

        // Pattern performance breakdown
        var patternPerfs = await _context.PatternPerformances
            .Where(p => p.UserId == UserId && p.TotalTrades >= 2)
            .OrderByDescending(p => p.TotalTrades)
            .Take(30)
            .ToListAsync();

        // Recent trades for context
        var recentTrades = await _simulation.GetTradeHistoryAsync(UserId, 30);

        // Per-timeframe stats
        var tfStats = recentTrades
            .Where(t => !string.IsNullOrEmpty(t.PatternTimeframe))
            .GroupBy(t => t.PatternTimeframe!)
            .Select(g =>
            {
                var wins = g.Count(t => t.Outcome == Domain.Enumerations.TradeOutcome.HitTarget1 ||
                                        t.Outcome == Domain.Enumerations.TradeOutcome.HitTarget2);
                var losses = g.Count(t => t.Outcome == Domain.Enumerations.TradeOutcome.HitStop);
                var total = wins + losses;
                return new
                {
                    Timeframe = g.Key,
                    Trades = g.Count(),
                    WinRate = total > 0 ? (decimal)wins / total * 100 : 0,
                    AvgR = g.Average(t => t.RMultiple ?? 0),
                    TotalPnL = g.Sum(t => t.PnLPercent ?? 0)
                };
            }).ToList();

        // Per-regime stats
        var regimeStats = recentTrades
            .GroupBy(t => t.RegimeAtEntry.ToString())
            .Select(g =>
            {
                var wins = g.Count(t => t.Outcome == Domain.Enumerations.TradeOutcome.HitTarget1 ||
                                        t.Outcome == Domain.Enumerations.TradeOutcome.HitTarget2);
                var losses = g.Count(t => t.Outcome == Domain.Enumerations.TradeOutcome.HitStop);
                var total = wins + losses;
                return new
                {
                    Regime = g.Key,
                    Trades = g.Count(),
                    WinRate = total > 0 ? (decimal)wins / total * 100 : 0,
                    AvgR = g.Average(t => t.RMultiple ?? 0)
                };
            }).ToList();

        // Per-asset stats
        var assetStats = recentTrades
            .GroupBy(t => t.Asset)
            .Select(g =>
            {
                var wins = g.Count(t => t.Outcome == Domain.Enumerations.TradeOutcome.HitTarget1 ||
                                        t.Outcome == Domain.Enumerations.TradeOutcome.HitTarget2);
                var losses = g.Count(t => t.Outcome == Domain.Enumerations.TradeOutcome.HitStop);
                var total = wins + losses;
                return new
                {
                    Symbol = g.Key,
                    Trades = g.Count(),
                    WinRate = total > 0 ? (decimal)wins / total * 100 : 0,
                    TotalPnL = g.Sum(t => t.PnLPercent ?? 0)
                };
            }).ToList();

        // ── Build the prompt ─────────────────────────────────────────
        var sb = new StringBuilder();
        sb.AppendLine("""
            You are Amplify's Strategy Advisor AI. You analyze a trader's full performance history 
            and provide data-driven recommendations to improve their trading system. You are direct,
            specific, and actionable. You back every recommendation with the trader's actual numbers.

            The trader is using Amplify, a system with:
            - Multi-timeframe pattern scanning (1H, 4H, Daily, Weekly)
            - 27 technical patterns (candlestick, chart patterns, indicator-based)
            - AI validation of patterns
            - Market regime detection (Trending, Choppy, VolExpansion, MeanReversion)
            - Real-time market data from Alpaca (stocks) and CoinGecko (crypto)
            - Simulated trade tracking with entry/stop/target

            Provide your analysis in these 4 sections. Be specific with numbers and pattern names.
            Use markdown formatting with ## headers for each section.
            """);

        // Section context based on request
        sb.AppendLine($"\nFocus area requested: {request.FocusArea ?? "All areas"}");
        sb.AppendLine();

        // Overall stats
        sb.AppendLine("=== OVERALL PERFORMANCE ===");
        sb.AppendLine($"Total Trades: {userStats.TotalTrades}");
        sb.AppendLine($"Win Rate: {userStats.WinRate:F1}%");
        sb.AppendLine($"Avg R-Multiple: {userStats.AvgRMultiple:F2}");
        sb.AppendLine($"Total P&L: {userStats.TotalPnLPercent:F1}%");
        sb.AppendLine($"Best Trade: {userStats.BestTrade:F1}%");
        sb.AppendLine($"Worst Trade: {userStats.WorstTrade:F1}%");
        sb.AppendLine($"Avg Days Held: {userStats.AvgDaysHeld:F1}");
        sb.AppendLine($"Long Win Rate: {userStats.LongWinRate:F1}%");
        sb.AppendLine($"Short Win Rate: {userStats.ShortWinRate:F1}%");
        sb.AppendLine($"Aligned TF Win Rate: {userStats.AlignedWinRate:F1}%");
        sb.AppendLine($"Conflicting TF Win Rate: {userStats.ConflictingWinRate:F1}%");

        if (signalStats != null)
        {
            sb.AppendLine($"\nAI Signals: {signalStats.AISignals} (Win Rate: {signalStats.AIWinRate:F1}%, Avg R: {signalStats.AIAvgRMultiple:F2}, Trades: {signalStats.AITradeCount})");
            sb.AppendLine($"Manual Signals: {signalStats.ManualSignals} (Win Rate: {signalStats.ManualWinRate:F1}%, Avg R: {signalStats.ManualAvgRMultiple:F2}, Trades: {signalStats.ManualTradeCount})");
        }

        // Per-timeframe breakdown
        sb.AppendLine("\n=== PERFORMANCE BY TIMEFRAME ===");
        foreach (var tf in tfStats.OrderByDescending(t => t.TotalPnL))
        {
            sb.AppendLine($"{tf.Timeframe}: {tf.Trades} trades, {tf.WinRate:F1}% win rate, Avg R: {tf.AvgR:F2}, Total P&L: {tf.TotalPnL:F1}%");
        }

        // Per-regime breakdown
        sb.AppendLine("\n=== PERFORMANCE BY MARKET REGIME ===");
        foreach (var r in regimeStats.OrderByDescending(r => r.WinRate))
        {
            sb.AppendLine($"{r.Regime}: {r.Trades} trades, {r.WinRate:F1}% win rate, Avg R: {r.AvgR:F2}");
        }

        // Per-asset breakdown
        sb.AppendLine("\n=== PERFORMANCE BY ASSET ===");
        foreach (var a in assetStats.OrderByDescending(a => a.TotalPnL))
        {
            sb.AppendLine($"{a.Symbol}: {a.Trades} trades, {a.WinRate:F1}% win rate, P&L: {a.TotalPnL:F1}%");
        }

        // Pattern performance
        sb.AppendLine("\n=== PATTERN PERFORMANCE (top by trade count) ===");
        foreach (var p in patternPerfs.Take(15))
        {
            sb.AppendLine($"{p.PatternType} ({p.Direction}) on {p.Timeframe} in {p.Regime}: " +
                $"{p.TotalTrades} trades, {p.WinRate:F1}% WR, Avg R: {p.AvgRMultiple:F2}, " +
                $"PF: {p.ProfitFactor:F2}, P&L: {p.TotalPnLPercent:F1}%");
            if (p.TradesWhenAligned > 0)
                sb.AppendLine($"  └─ When TF aligned: {p.WinRateWhenAligned:F1}% WR ({p.TradesWhenAligned} trades)");
            if (p.TradesWithBreakoutVol > 0)
                sb.AppendLine($"  └─ With breakout volume: {p.WinRateWithBreakoutVol:F1}% WR ({p.TradesWithBreakoutVol} trades)");
        }

        // Instructions per section
        sb.AppendLine("""

            ==========================================================================
            Based on ALL the data above, provide recommendations in these 4 sections:

            ## 1. Performance Feedback
            Analyze what's working and what isn't. Be specific:
            - Which timeframes should the trader focus on vs avoid?
            - Long vs short effectiveness
            - Impact of timeframe alignment on results
            - AI vs manual signal performance comparison
            - Identify the trader's 3 strongest and 3 weakest setups (by pattern+timeframe+regime)
            - Are there specific market regimes where the trader should avoid trading?

            ## 2. Strategy Tuning
            Based on the pattern performance data, recommend specific adjustments:
            - Which patterns should have higher/lower confidence thresholds?
            - Suggest minimum confidence scores per timeframe (e.g., "Only take 1H patterns above 85%")
            - Should the system require timeframe alignment for entries? (data shows aligned vs conflicting win rates)
            - Recommend stop loss / target adjustments based on avg win/loss sizes
            - Position sizing suggestions based on win rate and R-multiple data

            ## 3. New Indicators & Patterns
            Based on where the current system underperforms, suggest additions:
            - If volume is underused, recommend VWAP, OBV, or Volume Profile
            - If trend trades fail, suggest ADX, Ichimoku Cloud, or Supertrend
            - If reversal patterns underperform, suggest RSI divergence, Bollinger Band squeeze
            - If crypto trades underperform, suggest on-chain indicators or funding rate analysis
            - Recommend 2-3 specific indicators that would complement the current 27-pattern engine
            - For each, explain WHY it would help based on the trader's actual weaknesses

            ## 4. Data Source Recommendations
            Suggest market data sources that could improve signal quality:
            - If using free Alpaca IEX feed, explain benefits of upgrading to SIP feed
            - For crypto, evaluate CoinGecko vs alternatives (Binance WebSocket, CoinMarketCap)
            - Recommend sentiment data sources (Fear & Greed Index, social media sentiment)
            - Suggest economic calendar integration for avoiding news events
            - Options flow data sources if options trading is relevant
            - Each recommendation should include: name, what it provides, cost tier, and expected impact

            IMPORTANT: Ground every recommendation in the trader's actual data. 
            Don't make generic suggestions. If a pattern has 80% win rate on Daily but 30% on 1H,
            say exactly that and recommend focusing on Daily for that pattern.
            If there's insufficient data (< 5 trades), say so and suggest what to test next.
            """);

        try
        {
            var response = await _advisor.GetAdvisoryAsync(sb.ToString());
            return Ok(new StrategyAnalysisResponse
            {
                Analysis = response,
                GeneratedAt = DateTime.UtcNow,
                TradesAnalyzed = userStats.TotalTrades,
                PatternsAnalyzed = patternPerfs.Count,
                // Summary stats for the UI header
                OverallWinRate = userStats.WinRate,
                AvgRMultiple = userStats.AvgRMultiple,
                TotalPnL = userStats.TotalPnLPercent,
                BestTimeframe = tfStats.OrderByDescending(t => t.WinRate).FirstOrDefault()?.Timeframe ?? "N/A",
                WorstTimeframe = tfStats.OrderBy(t => t.WinRate).FirstOrDefault()?.Timeframe ?? "N/A",
                BestRegime = regimeStats.OrderByDescending(r => r.WinRate).FirstOrDefault()?.Regime ?? "N/A",
                WorstRegime = regimeStats.OrderBy(r => r.WinRate).FirstOrDefault()?.Regime ?? "N/A"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = $"AI service unavailable: {ex.Message}" });
        }
    }
}

public class StrategyAnalysisRequest
{
    public string? FocusArea { get; set; } // "Performance", "Strategy", "Indicators", "DataSources", or null for all
}

public class StrategyAnalysisResponse
{
    public string Analysis { get; set; } = "";
    public DateTime GeneratedAt { get; set; }
    public int TradesAnalyzed { get; set; }
    public int PatternsAnalyzed { get; set; }
    public decimal OverallWinRate { get; set; }
    public decimal AvgRMultiple { get; set; }
    public decimal TotalPnL { get; set; }
    public string BestTimeframe { get; set; } = "";
    public string WorstTimeframe { get; set; } = "";
    public string BestRegime { get; set; } = "";
    public string WorstRegime { get; set; } = "";
}