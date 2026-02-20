using Amplify.Application.Common.Interfaces.AI;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
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
public class PatternScannerController : ControllerBase
{
    private readonly IPatternDetector _detector;
    private readonly IPatternAnalyzer _analyzer;
    private readonly ApplicationDbContext _context;

    public PatternScannerController(IPatternDetector detector, IPatternAnalyzer analyzer, ApplicationDbContext context)
    {
        _detector = detector;
        _analyzer = analyzer;
        _context = context;
    }

    /// <summary>
    /// Scan a symbol for patterns, then run AI analysis on results.
    /// </summary>
    [HttpPost("scan")]
    public async Task<IActionResult> ScanSymbol([FromBody] ScanRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        // Generate sample candles (replace with Alpaca data later)
        var candles = GenerateSampleCandles(request.Symbol, 250);

        // Run pattern detection
        var patterns = _detector.DetectAll(candles);

        // Filter
        if (request.MinConfidence > 0)
            patterns = patterns.Where(p => p.Confidence >= request.MinConfidence).ToList();

        if (!string.IsNullOrEmpty(request.Direction) && request.Direction != "All")
        {
            if (Enum.TryParse<PatternDirection>(request.Direction, out var dir))
                patterns = patterns.Where(p => p.Direction == dir).ToList();
        }

        // Run AI synthesis on all patterns
        MultiPatternAnalysis? aiSynthesis = null;
        if (patterns.Any() && request.EnableAI)
        {
            try
            {
                aiSynthesis = await _analyzer.SynthesizePatternsAsync(patterns, candles, request.Symbol.ToUpper());
            }
            catch { /* AI unavailable — continue without it */ }
        }

        // Save detected patterns to DB
        foreach (var p in patterns)
        {
            var verdict = aiSynthesis?.PatternVerdicts.FirstOrDefault(v =>
                v.PatternName.Equals(p.PatternName, StringComparison.OrdinalIgnoreCase));

            var entity = new DetectedPattern
            {
                Asset = request.Symbol.ToUpper(),
                PatternType = p.PatternType,
                Direction = p.Direction,
                Timeframe = Enum.TryParse<PatternTimeframe>(request.Timeframe, out var tf) ? tf : PatternTimeframe.Daily,
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
                UserId = userId
            };
            _context.DetectedPatterns.Add(entity);
        }
        await _context.SaveChangesAsync();

        return Ok(new ScanResponse
        {
            Symbol = request.Symbol.ToUpper(),
            TotalPatterns = patterns.Count,
            CurrentPrice = candles.Last().Close,
            Patterns = patterns.Select(p =>
            {
                var verdict = aiSynthesis?.PatternVerdicts.FirstOrDefault(v =>
                    v.PatternName.Equals(p.PatternName, StringComparison.OrdinalIgnoreCase));

                return new PatternDto
                {
                    PatternName = p.PatternName,
                    PatternType = p.PatternType.ToString(),
                    Direction = p.Direction.ToString(),
                    Confidence = Math.Round(p.Confidence, 1),
                    HistoricalWinRate = p.HistoricalWinRate,
                    Description = p.Description,
                    SuggestedEntry = Math.Round(p.SuggestedEntry, 2),
                    SuggestedStop = Math.Round(p.SuggestedStop, 2),
                    SuggestedTarget = Math.Round(p.SuggestedTarget, 2),
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    AIGrade = verdict?.Grade ?? "",
                    AIValid = verdict?.IsValid ?? true,
                    AIReason = verdict?.OneLineReason ?? ""
                };
            }).ToList(),
            AIAnalysis = aiSynthesis is not null ? new AIAnalysisDto
            {
                OverallBias = aiSynthesis.OverallBias,
                OverallConfidence = Math.Round(aiSynthesis.OverallConfidence, 1),
                Summary = aiSynthesis.Summary,
                DetailedAnalysis = aiSynthesis.DetailedAnalysis,
                RecommendedAction = aiSynthesis.RecommendedAction,
                RecommendedEntry = aiSynthesis.RecommendedEntry,
                RecommendedStop = aiSynthesis.RecommendedStop,
                RecommendedTarget = aiSynthesis.RecommendedTarget,
                RiskReward = aiSynthesis.RiskReward
            } : null
        });
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory([FromQuery] string? symbol)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var query = _context.DetectedPatterns
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .AsQueryable();

        if (!string.IsNullOrEmpty(symbol))
            query = query.Where(p => p.Asset == symbol.ToUpper());

        var results = await query.Take(100).Select(p => new
        {
            p.Id,
            p.Asset,
            PatternType = p.PatternType.ToString(),
            Direction = p.Direction.ToString(),
            p.Confidence,
            p.HistoricalWinRate,
            p.Description,
            p.DetectedAtPrice,
            p.SuggestedEntry,
            p.SuggestedStop,
            p.SuggestedTarget,
            p.AIAnalysis,
            p.AIConfidence,
            p.WasCorrect,
            p.ActualPnLPercent,
            p.CreatedAt
        }).ToListAsync();

        return Ok(results);
    }

    private List<Candle> GenerateSampleCandles(string symbol, int count)
    {
        var candles = new List<Candle>();
        var random = new Random(symbol.GetHashCode());

        var basePrice = symbol.ToUpper() switch
        {
            "TSLA" => 410m,
            "AAPL" => 225m,
            "MSFT" => 420m,
            "GOOGL" => 175m,
            "AMZN" => 200m,
            "NVDA" => 870m,
            "META" => 590m,
            "BTC" => 95000m,
            _ => 150m
        };

        var price = basePrice * 0.85m;

        for (int i = count; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i);
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;

            var change = (decimal)(random.NextDouble() - 0.47) * basePrice * 0.02m;
            var open = price;
            var close = price + change;
            var high = Math.Max(open, close) + (decimal)random.NextDouble() * basePrice * 0.008m;
            var low = Math.Min(open, close) - (decimal)random.NextDouble() * basePrice * 0.008m;
            var volume = 5000000m + (decimal)random.Next(0, 15000000);

            candles.Add(new Candle
            {
                Time = date,
                Open = Math.Round(open, 2),
                High = Math.Round(high, 2),
                Low = Math.Round(low, 2),
                Close = Math.Round(close, 2),
                Volume = volume
            });
            price = close;
        }
        return candles;
    }
}

// ===== DTOs =====

public class ScanRequest
{
    public string Symbol { get; set; } = "";
    public decimal MinConfidence { get; set; } = 0;
    public string Direction { get; set; } = "All";
    public string Timeframe { get; set; } = "Daily";
    public bool EnableAI { get; set; } = true;
}

public class ScanResponse
{
    public string Symbol { get; set; } = "";
    public int TotalPatterns { get; set; }
    public decimal CurrentPrice { get; set; }
    public List<PatternDto> Patterns { get; set; } = new();
    public AIAnalysisDto? AIAnalysis { get; set; }
}

public class PatternDto
{
    public string PatternName { get; set; } = "";
    public string PatternType { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal Confidence { get; set; }
    public decimal HistoricalWinRate { get; set; }
    public string Description { get; set; } = "";
    public decimal SuggestedEntry { get; set; }
    public decimal SuggestedStop { get; set; }
    public decimal SuggestedTarget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    // AI fields
    public string AIGrade { get; set; } = "";
    public bool AIValid { get; set; } = true;
    public string AIReason { get; set; } = "";
}

public class AIAnalysisDto
{
    public string OverallBias { get; set; } = "";
    public decimal OverallConfidence { get; set; }
    public string Summary { get; set; } = "";
    public string DetailedAnalysis { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
    public decimal? RecommendedEntry { get; set; }
    public decimal? RecommendedStop { get; set; }
    public decimal? RecommendedTarget { get; set; }
    public string RiskReward { get; set; } = "";
}