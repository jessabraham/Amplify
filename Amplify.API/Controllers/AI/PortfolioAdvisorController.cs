using Amplify.Application.Common.Interfaces.AI;
using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace Amplify.API.Controllers.AI;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioAdvisorController : ControllerBase
{
    private readonly IAIAdvisor _advisor;
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _config;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public PortfolioAdvisorController(
        IAIAdvisor advisor,
        ApplicationDbContext context,
        IConfiguration config)
    {
        _advisor = advisor;
        _context = context;
        _config = config;
    }

    /// <summary>
    /// Get AI-powered portfolio allocation suggestions based on watchlist, open positions, and cash available.
    /// </summary>
    [HttpPost("allocate")]
    public async Task<IActionResult> GetAllocationAdvice()
    {
        try
        {
            // ── Gather portfolio data ────────────────────────────────
            var user = await _context.Users
                .OfType<ApplicationUser>()
                .FirstOrDefaultAsync(u => u.Id == UserId);

            if (user is null) return Unauthorized();

            var watchlist = await _context.Set<WatchlistItem>()
                .Where(w => w.UserId == UserId && w.IsActive)
                .ToListAsync();

            if (!watchlist.Any())
                return BadRequest("Add symbols to your watchlist first.");

            var openPositions = await _context.Positions
                .Where(p => p.UserId == UserId && p.Status == PositionStatus.Open && p.IsActive)
                .ToListAsync();

            var realizedPnL = 0m;
            try
            {
                realizedPnL = await _context.SimulatedTrades
                    .Where(t => t.UserId == UserId && t.Status == SimulationStatus.Resolved)
                    .SumAsync(t => t.PnLDollars ?? 0);
            }
            catch { }

            var totalInvested = openPositions.Sum(p => p.EntryPrice * p.Quantity);
            var cashAvailable = user.StartingCapital + realizedPnL - totalInvested;

            // ── Get recent scan data for watchlist symbols ───────────
            var recentSignals = await _context.TradeSignals
                .Where(s => s.UserId == UserId && watchlist.Select(w => w.Symbol).Contains(s.Asset))
                .OrderByDescending(s => s.CreatedAt)
                .Take(20)
                .Select(s => new { s.Asset, s.SignalType, s.PatternName, s.PatternConfidence, s.AIConfidence, s.AIBias, s.SetupScore, s.Regime })
                .ToListAsync();

            // ── Get LIVE patterns only (Active or PlayingOut) ───────
            var livePatterns = await _context.DetectedPatterns
                .Where(p => p.UserId == UserId
                    && (p.Status == Domain.Enumerations.PatternStatus.Active || p.Status == Domain.Enumerations.PatternStatus.PlayingOut)
                    && watchlist.Select(w => w.Symbol).Contains(p.Asset))
                .OrderByDescending(p => p.Confidence)
                .ToListAsync();

            // ── Get recent regime data ──────────────────────────────
            var recentRegimes = await _context.RegimeHistory
                .Where(r => watchlist.Select(w => w.Symbol).Contains(r.Symbol))
                .GroupBy(r => r.Symbol)
                .Select(g => new { Symbol = g.Key, Regime = g.OrderByDescending(r => r.DetectedAt).First().Regime, Confidence = g.OrderByDescending(r => r.DetectedAt).First().Confidence })
                .ToListAsync();

            // ── Risk settings ───────────────────────────────────────
            var defaultRiskPct = double.TryParse(_config["Risk:DefaultRiskPercent"], out var rp) ? rp : 2.0;
            var maxPositionPct = 25.0;

            // ── Build AI prompt (kept concise for Qwen3 8B speed) ───
            var prompt = new StringBuilder();
            prompt.AppendLine("Respond with ONLY valid JSON. No markdown, no text outside JSON.");
            prompt.AppendLine();
            prompt.AppendLine($"Cash: ${cashAvailable:N0}. Max per position: 25%. Positions open: {openPositions.Count}.");

            if (openPositions.Any())
            {
                prompt.Append("Current positions: ");
                prompt.AppendLine(string.Join(", ", openPositions.Select(p => $"{p.Symbol}(${p.EntryPrice * p.Quantity:N0})")));
            }

            prompt.AppendLine();
            prompt.AppendLine("Evaluate each symbol (only LIVE patterns — Active or PlayingOut):");
            foreach (var w in watchlist)
            {
                var regime = recentRegimes.FirstOrDefault(r => r.Symbol == w.Symbol);
                var symbolPatterns = livePatterns.Where(p => p.Asset == w.Symbol).ToList();
                var hasPosition = openPositions.Any(p => p.Symbol == w.Symbol);

                prompt.Append($"- {w.Symbol}: {symbolPatterns.Count} live patterns, regime={regime?.Regime.ToString() ?? "Unknown"}, bias={w.LastBias ?? "N/A"}");
                if (symbolPatterns.Any())
                {
                    foreach (var pat in symbolPatterns.Take(5))
                    {
                        prompt.Append($"\n  [{pat.Id.ToString()[..8]}] {pat.PatternType}({pat.Direction}) {pat.Timeframe} conf={pat.Confidence:F0}% status={pat.Status}");
                        if (pat.SuggestedEntry > 0) prompt.Append($" entry=${pat.SuggestedEntry:F2}");
                        if (pat.SuggestedTarget > 0) prompt.Append($" tgt=${pat.SuggestedTarget:F2}");
                        if (pat.SuggestedStop > 0) prompt.Append($" stop=${pat.SuggestedStop:F2}");
                    }
                }
                if (hasPosition)
                    prompt.Append("\n  HAS_POSITION");
                prompt.AppendLine();
            }

            prompt.AppendLine();
            prompt.AppendLine(@"JSON format:
{""summary"":""1 sentence"",""totalSuggestedAllocation"":0,""cashRetained"":0,""allocations"":[{""symbol"":""X"",""suggestedBudget"":0,""direction"":""Long"",""confidence"":""High"",""rationale"":""why"",""riskNote"":""risk"",""portfolioPercent"":0,""skip"":false,""skipReason"":null,""patternIds"":[""id1""],""patternSummary"":""BullishEngulfing Daily 82%""}],""warnings"":[],""diversificationScore"":""Good""}");

            // ── Call AI ─────────────────────────────────────────────
            var response = await _advisor.GetAdvisoryAsync(prompt.ToString());

            // ── Parse JSON from response ────────────────────────────
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0 || jsonEnd <= jsonStart)
                return Ok(new { raw = response, error = "AI did not return valid JSON." });

            var json = response[jsonStart..(jsonEnd + 1)];

            var result = JsonSerializer.Deserialize<JsonElement>(json);

            // ── Save advice to history ──────────────────────────────
            try
            {
                var summary = result.TryGetProperty("summary", out var s) ? s.GetString() ?? "" : "";
                var divScore = result.TryGetProperty("diversificationScore", out var d) ? d.GetString() ?? "" : "";
                var totalAlloc = result.TryGetProperty("totalSuggestedAllocation", out var ta) ? ta.GetDecimal() : 0;
                var retained = result.TryGetProperty("cashRetained", out var cr) ? cr.GetDecimal() : 0;
                var allocCount = result.TryGetProperty("allocations", out var allocs) ? allocs.GetArrayLength() : 0;

                var advice = new Domain.Entities.AI.PortfolioAdvice
                {
                    CashAvailable = cashAvailable,
                    TotalInvested = totalInvested,
                    OpenPositionCount = openPositions.Count,
                    WatchlistCount = watchlist.Count,
                    Summary = summary,
                    DiversificationScore = divScore,
                    TotalSuggestedAllocation = totalAlloc,
                    CashRetained = retained,
                    ResponseJson = json,
                    TotalAllocations = allocCount,
                    UserId = UserId
                };

                _context.PortfolioAdvices.Add(advice);
                await _context.SaveChangesAsync();
            }
            catch { /* saving history is best-effort */ }

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get past portfolio advice history.
    /// </summary>
    [HttpGet("history")]
    public async Task<IActionResult> GetAdviceHistory([FromQuery] int count = 10)
    {
        var history = await _context.PortfolioAdvices
            .Where(a => a.UserId == UserId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(Math.Min(count, 50))
            .Select(a => new
            {
                a.Id,
                a.CreatedAt,
                a.Summary,
                a.DiversificationScore,
                a.TotalSuggestedAllocation,
                a.CashRetained,
                a.CashAvailable,
                a.TotalInvested,
                a.OpenPositionCount,
                a.WatchlistCount,
                a.TotalAllocations,
                a.AllocationsFollowed,
                a.ResponseJson
            })
            .ToListAsync();

        return Ok(history);
    }

    /// <summary>
    /// Get advisor accuracy scorecard — cross-references past advice with position outcomes.
    /// </summary>
    [HttpGet("scorecard")]
    public async Task<IActionResult> GetScorecard()
    {
        try
        {
            var adviceList = await _context.PortfolioAdvices
                .Where(a => a.UserId == UserId)
                .OrderByDescending(a => a.CreatedAt)
                .Take(50)
                .ToListAsync();

            var allPositions = await _context.Positions
                .Where(p => p.UserId == UserId)
                .ToListAsync();

            var results = new List<object>();
            var byConfidence = new Dictionary<string, (int total, int executed, int profitable, decimal totalPnL)>(StringComparer.OrdinalIgnoreCase)
            {
                ["High"] = (0, 0, 0, 0),
                ["Medium"] = (0, 0, 0, 0),
                ["Low"] = (0, 0, 0, 0)
            };

            var totalAllocations = 0;
            var totalExecuted = 0;
            var totalProfitable = 0;
            var totalPnL = 0m;
            var aiExecuted = 0;
            var manualExecuted = 0;

            foreach (var advice in adviceList)
            {
                try
                {
                    using var doc = JsonDocument.Parse(advice.ResponseJson);
                    if (!doc.RootElement.TryGetProperty("allocations", out var allocs)) continue;

                    foreach (var alloc in allocs.EnumerateArray())
                    {
                        var skip = alloc.TryGetProperty("skip", out var sp) && sp.GetBoolean();
                        if (skip) continue;

                        var symbol = alloc.TryGetProperty("symbol", out var sym) ? sym.GetString() ?? "" : "";
                        var confidence = alloc.TryGetProperty("confidence", out var conf) ? conf.GetString() ?? "Medium" : "Medium";
                        if (string.IsNullOrEmpty(symbol)) continue;

                        // Normalize confidence
                        if (!byConfidence.ContainsKey(confidence)) confidence = "Medium";

                        totalAllocations++;

                        // Find matching position (created within 12h after advice)
                        var windowEnd = advice.CreatedAt.AddHours(12);
                        var match = allPositions
                            .Where(p => p.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase)
                                && p.CreatedAt >= advice.CreatedAt.AddMinutes(-5)
                                && p.CreatedAt <= windowEnd)
                            .OrderBy(p => p.CreatedAt)
                            .FirstOrDefault();

                        var executed = match is not null;
                        var pnl = 0m;
                        var profitable = false;

                        if (executed)
                        {
                            totalExecuted++;
                            if (match!.IsAiGenerated) aiExecuted++;
                            else manualExecuted++;

                            pnl = match.Status == PositionStatus.Closed
                                ? match.RealizedPnL
                                : match.UnrealizedPnL;
                            profitable = pnl > 0;
                            if (profitable) totalProfitable++;
                            totalPnL += pnl;
                        }

                        var entry = byConfidence[confidence];
                        byConfidence[confidence] = (
                            entry.total + 1,
                            entry.executed + (executed ? 1 : 0),
                            entry.profitable + (profitable ? 1 : 0),
                            entry.totalPnL + pnl
                        );
                    }
                }
                catch { /* skip malformed advice */ }
            }

            return Ok(new
            {
                AdviceCount = adviceList.Count,
                TotalAllocations = totalAllocations,
                TotalExecuted = totalExecuted,
                TotalProfitable = totalProfitable,
                ExecutionRate = totalAllocations > 0 ? Math.Round((decimal)totalExecuted / totalAllocations * 100, 1) : 0,
                WinRate = totalExecuted > 0 ? Math.Round((decimal)totalProfitable / totalExecuted * 100, 1) : 0,
                TotalPnL = Math.Round(totalPnL, 2),
                AiExecuted = aiExecuted,
                ManualExecuted = manualExecuted,
                ByConfidence = byConfidence.Select(kv => new
                {
                    Confidence = kv.Key,
                    Total = kv.Value.total,
                    Executed = kv.Value.executed,
                    Profitable = kv.Value.profitable,
                    WinRate = kv.Value.executed > 0 ? Math.Round((decimal)kv.Value.profitable / kv.Value.executed * 100, 1) : 0,
                    TotalPnL = Math.Round(kv.Value.totalPnL, 2)
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}