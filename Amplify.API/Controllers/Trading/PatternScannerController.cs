using Amplify.Application.Common.Interfaces.AI;
using Amplify.Application.Common.Interfaces.Market;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Amplify.Infrastructure.Services;
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
    private readonly TradeSimulationService _simulation;
    private readonly IMarketDataService _marketData;

    public PatternScannerController(
        IPatternDetector detector,
        IPatternAnalyzer analyzer,
        ApplicationDbContext context,
        TradeSimulationService simulation,
        IMarketDataService marketData)
    {
        _detector = detector;
        _analyzer = analyzer;
        _context = context;
        _simulation = simulation;
        _marketData = marketData;
    }

    // ═══════════════════════════════════════════════════════════════════
    // MULTI-TIMEFRAME SCAN
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost("scan")]
    public async Task<IActionResult> ScanSymbol([FromBody] ScanRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var symbol = request.Symbol.ToUpper();

        // ── Fetch candles for each timeframe (Alpaca → sample data fallback) ──
        var candles1H = await _marketData.GetCandlesAsync(symbol, 200, "1H");
        var candles4H = await _marketData.GetCandlesAsync(symbol, 200, "4H");
        var candlesDaily = await _marketData.GetCandlesAsync(symbol, 250, "Daily");
        var candlesWeekly = await _marketData.GetCandlesAsync(symbol, 104, "Weekly");

        // ── Detect patterns per timeframe ────────────────────────────
        var patterns1H = _detector.DetectAll(candles1H);
        patterns1H.ForEach(p => p.Timeframe = "1H");

        var patterns4H = _detector.DetectAll(candles4H);
        patterns4H.ForEach(p => p.Timeframe = "4H");

        var patternsDaily = _detector.DetectAll(candlesDaily);
        patternsDaily.ForEach(p => p.Timeframe = "Daily");

        var patternsWeekly = _detector.DetectAll(candlesWeekly);
        patternsWeekly.ForEach(p => p.Timeframe = "Weekly");

        Console.WriteLine($"📊 Scan {symbol}: 1H={candles1H.Count} candles/{patterns1H.Count} patterns, " +
            $"4H={candles4H.Count}/{patterns4H.Count}, Daily={candlesDaily.Count}/{patternsDaily.Count}, " +
            $"Weekly={candlesWeekly.Count}/{patternsWeekly.Count}");

        // ── Build context per timeframe ──────────────────────────────
        var ctx1H = BuildMarketContext(candles1H, "1H");
        var ctx4H = BuildMarketContext(candles4H, "4H");
        var ctxDaily = BuildMarketContext(candlesDaily, "Daily");
        var ctxWeekly = BuildMarketContext(candlesWeekly, "Weekly");

        // ── Deduplicate patterns per timeframe ───────────────────────
        var dedup1H = DeduplicatePatterns(patterns1H);
        var dedup4H = DeduplicatePatterns(patterns4H);
        var dedupDaily = DeduplicatePatterns(patternsDaily);
        var dedupWeekly = DeduplicatePatterns(patternsWeekly);

        Console.WriteLine($"📊 After dedup: 1H={patterns1H.Count}→{dedup1H.Count}, " +
            $"4H={patterns4H.Count}→{dedup4H.Count}, Daily={patternsDaily.Count}→{dedupDaily.Count}, " +
            $"Weekly={patternsWeekly.Count}→{dedupWeekly.Count}");

        // ── Apply timeframe weights to confidence ────────────────────
        // Gentle scaling — don't penalize short timeframes too harshly
        foreach (var p in dedup1H) p.Confidence = Math.Min(p.Confidence * 0.85m, 95);
        foreach (var p in dedup4H) p.Confidence = Math.Min(p.Confidence * 0.90m, 95);
        foreach (var p in dedupWeekly) p.Confidence = Math.Min(p.Confidence * 1.05m, 98);

        // ── Build timeframe data ─────────────────────────────────────
        var tf1H = BuildTimeframeData("1H", 0.5m, dedup1H, ctx1H);
        var tf4H = BuildTimeframeData("4H", 1.0m, dedup4H, ctx4H);
        var tfDaily = BuildTimeframeData("Daily", 2.0m, dedupDaily, ctxDaily);
        var tfWeekly = BuildTimeframeData("Weekly", 3.0m, dedupWeekly, ctxWeekly);

        var timeframes = new List<TimeframeData> { tf1H, tf4H, tfDaily, tfWeekly };

        // ── Combined regime (weighted) ───────────────────────────────
        var (combinedRegime, regimeConf, regimeAlign) = CombineRegimes(timeframes);

        // ── Direction alignment ──────────────────────────────────────
        var (dirAlign, alignScore) = CalculateDirectionAlignment(timeframes);

        // ── All patterns from all timeframes (deduped per timeframe) ──
        var topPatterns = dedup1H.Concat(dedup4H).Concat(dedupDaily).Concat(dedupWeekly)
            .OrderByDescending(p => p.Confidence)
            .ToList();

        Console.WriteLine($"📊 All patterns: 1H={topPatterns.Count(p => p.Timeframe == "1H")}, " +
            $"4H={topPatterns.Count(p => p.Timeframe == "4H")}, " +
            $"Daily={topPatterns.Count(p => p.Timeframe == "Daily")}, " +
            $"Weekly={topPatterns.Count(p => p.Timeframe == "Weekly")}");

        // ── Remove contradictory patterns on overlapping candles ────
        // Can't have opposite signals on the same date range within the SAME timeframe
        var contradictionPairs = new[]
        {
            // Candlestick opposites
            ("Bullish Engulfing", "Bearish Engulfing"),
            ("Bullish Harami", "Bearish Harami"),
            ("Morning Star", "Evening Star"),
            ("Three White Soldiers", "Three Black Crows"),
            ("Hammer", "Shooting Star"),
            ("Inverted Hammer", "Shooting Star"),
            ("Hammer", "Inverted Hammer"),
            ("Piercing Line", "Dark Cloud Cover"),
            // Chart pattern opposites
            ("Double Bottom", "Double Top"),
            ("Head and Shoulders", "Inverse Head and Shoulders"),
            // Technical indicator opposites
            ("Golden Cross", "Death Cross"),
            ("MACD Bullish Cross", "MACD Bearish Cross"),
            ("RSI Overbought", "RSI Oversold"),
            // Cross-category conflicts
            ("Hammer", "Evening Star"),
            ("Hammer", "Bearish Engulfing"),
            ("Hammer", "Dark Cloud Cover"),
            ("Hammer", "Three Black Crows"),
            ("Morning Star", "Shooting Star"),
            ("Morning Star", "Bearish Engulfing"),
            ("Morning Star", "Dark Cloud Cover"),
            ("Bullish Engulfing", "Evening Star"),
            ("Bullish Engulfing", "Shooting Star"),
            ("Bullish Engulfing", "Dark Cloud Cover"),
            ("Three White Soldiers", "Evening Star"),
            ("Three White Soldiers", "Shooting Star"),
            ("Three White Soldiers", "Bearish Engulfing"),
            ("Piercing Line", "Evening Star"),
            ("Piercing Line", "Shooting Star")
        };

        // Process contradictions PER TIMEFRAME — a 1H Hammer doesn't conflict with a Weekly Shooting Star
        foreach (var tf in new[] { "1H", "4H", "Daily", "Weekly" })
        {
            var tfPatterns2 = topPatterns.Where(p => p.Timeframe == tf).ToList();
            foreach (var (nameA, nameB) in contradictionPairs)
            {
                var patA = tfPatterns2.FirstOrDefault(p => p.PatternName == nameA);
                var patB = tfPatterns2.FirstOrDefault(p => p.PatternName == nameB);
                if (patA != null && patB != null)
                {
                    var dateGap = Math.Abs((patA.EndDate - patB.EndDate).TotalDays);
                    if (dateGap < 14)
                    {
                        if (patA.Confidence >= patB.Confidence)
                            topPatterns.Remove(patB);
                        else
                            topPatterns.Remove(patA);
                    }
                }
            }
        }

        Console.WriteLine($"📊 After contradictions: 1H={topPatterns.Count(p => p.Timeframe == "1H")}, " +
            $"4H={topPatterns.Count(p => p.Timeframe == "4H")}, " +
            $"Daily={topPatterns.Count(p => p.Timeframe == "Daily")}, " +
            $"Weekly={topPatterns.Count(p => p.Timeframe == "Weekly")}");
        Console.WriteLine($"📊 Pattern details: {string.Join(" | ", topPatterns.Select(p => $"[{p.Timeframe}] {p.PatternName} {p.Confidence:F0}%"))}");

        // ── General direction conflict: if multiple patterns on same timeframe
        // within 7 days point in opposite directions, keep the higher confidence ones
        var toRemove = new List<PatternResult>();
        foreach (var p in topPatterns)
        {
            if (toRemove.Contains(p)) continue;
            var conflicts = topPatterns.Where(other =>
                other != p && !toRemove.Contains(other) &&
                other.Timeframe == p.Timeframe &&
                other.Direction != p.Direction &&
                p.Direction != PatternDirection.Neutral &&
                other.Direction != PatternDirection.Neutral &&
                Math.Abs((other.EndDate - p.EndDate).TotalDays) < 7
            ).ToList();

            foreach (var c in conflicts)
            {
                if (c.Confidence < p.Confidence)
                    toRemove.Add(c);
            }
        }
        topPatterns.RemoveAll(p => toRemove.Contains(p));

        // ── Key price levels from daily ──────────────────────────────
        var keyLevels = DetectKeyLevels(candlesDaily);
        ctxDaily.KeyLevels = keyLevels;
        var price = candlesDaily.Last().Close;
        var supports = keyLevels.Where(l => l.Price < price).OrderByDescending(l => l.Price).ToList();
        var resistances = keyLevels.Where(l => l.Price > price).OrderBy(l => l.Price).ToList();
        ctxDaily.NearestSupport = supports.FirstOrDefault()?.Price;
        ctxDaily.NearestResistance = resistances.FirstOrDefault()?.Price;
        if (ctxDaily.NearestSupport.HasValue)
            ctxDaily.DistanceToSupportPct = (price - ctxDaily.NearestSupport.Value) / price * 100;
        if (ctxDaily.NearestResistance.HasValue)
            ctxDaily.DistanceToResistancePct = (ctxDaily.NearestResistance.Value - price) / price * 100;

        // ── AI synthesis with full context ───────────────────────────
        MultiPatternAnalysis? aiSynthesis = null;
        if (topPatterns.Any() && request.EnableAI)
        {
            try
            {
                // Pull user's historical performance for relevant patterns
                List<PatternPerformanceData>? perfStats = null;
                UserStatsData? userStats = null;

                if (!string.IsNullOrEmpty(userId))
                {
                    try
                    {
                        var patternTypes = topPatterns
                            .Where(p => Enum.TryParse<PatternType>(p.PatternName.Replace(" ", ""), out _))
                            .Select(p => Enum.Parse<PatternType>(p.PatternName.Replace(" ", "")))
                            .Distinct().ToList();

                        var regime = Enum.TryParse<MarketRegime>(combinedRegime, out var reg) ? reg : MarketRegime.Choppy;
                        var rawPerfs = await _simulation.GetRelevantStatsAsync(userId, patternTypes, regime);

                        if (rawPerfs.Any())
                        {
                            perfStats = rawPerfs.Select(p => new PatternPerformanceData
                            {
                                PatternName = p.PatternType.ToString(),
                                Direction = p.Direction.ToString(),
                                Timeframe = p.Timeframe,
                                Regime = p.Regime.ToString(),
                                TotalTrades = p.TotalTrades,
                                Wins = p.Wins,
                                Losses = p.Losses,
                                WinRate = p.WinRate,
                                AvgRMultiple = p.AvgRMultiple,
                                ProfitFactor = p.ProfitFactor,
                                WinRateWhenAligned = p.WinRateWhenAligned,
                                TradesWhenAligned = p.TradesWhenAligned,
                                WinRateWhenConflicting = p.WinRateWhenConflicting,
                                TradesWhenConflicting = p.TradesWhenConflicting,
                                WinRateWithBreakoutVol = p.WinRateWithBreakoutVol,
                                TradesWithBreakoutVol = p.TradesWithBreakoutVol,
                                AvgDaysHeld = p.AvgDaysHeld
                            }).ToList();
                        }

                        var rawUserStats = await _simulation.GetUserStatsAsync(userId);
                        if (rawUserStats.TotalTrades > 0)
                        {
                            userStats = new UserStatsData
                            {
                                TotalTrades = rawUserStats.TotalTrades,
                                WinRate = rawUserStats.WinRate,
                                AvgRMultiple = rawUserStats.AvgRMultiple,
                                LongWinRate = rawUserStats.LongWinRate,
                                ShortWinRate = rawUserStats.ShortWinRate,
                                AlignedWinRate = rawUserStats.AlignedWinRate,
                                ConflictingWinRate = rawUserStats.ConflictingWinRate
                            };
                        }
                    }
                    catch { /* stats are non-critical */ }
                }

                aiSynthesis = await _analyzer.SynthesizeMultiTimeframeAsync(
                    topPatterns.Take(12).ToList(), timeframes, ctxDaily, combinedRegime, regimeConf, dirAlign, alignScore, symbol,
                    perfStats, userStats);
            }
            catch { /* AI unavailable */ }
        }

        // Fallback if AI unavailable
        if (aiSynthesis is null && topPatterns.Any())
        {
            var bullish = topPatterns.Count(p => p.Direction == PatternDirection.Bullish);
            var bearish = topPatterns.Count(p => p.Direction == PatternDirection.Bearish);
            aiSynthesis = new MultiPatternAnalysis
            {
                Symbol = symbol,
                OverallBias = bullish > bearish ? "Bullish" : bearish > bullish ? "Bearish" : "Neutral",
                OverallConfidence = topPatterns.Average(p => p.Confidence),
                Summary = "AI synthesis unavailable — showing math-only analysis.",
                RecommendedAction = "Watch"
            };
        }

        // ── Save top patterns to DB ──────────────────────────────────
        foreach (var p in topPatterns)
        {
            var verdict = aiSynthesis?.PatternVerdicts.FirstOrDefault(v =>
                v.PatternName.Equals(p.PatternName, StringComparison.OrdinalIgnoreCase));

            _context.DetectedPatterns.Add(new DetectedPattern
            {
                Asset = symbol,
                PatternType = p.PatternType,
                Direction = p.Direction,
                Timeframe = Enum.TryParse<PatternTimeframe>(p.Timeframe, out var tf) ? tf : PatternTimeframe.Daily,
                Confidence = p.Confidence,
                HistoricalWinRate = p.HistoricalWinRate,
                Description = p.Description,
                DetectedAtPrice = price,
                SuggestedEntry = p.SuggestedEntry,
                SuggestedStop = p.SuggestedStop,
                SuggestedTarget = p.SuggestedTarget,
                PatternStartDate = p.StartDate,
                PatternEndDate = p.EndDate,
                AIAnalysis = verdict is not null ? $"[{verdict.Grade}] {verdict.OneLineReason}" : null,
                AIConfidence = aiSynthesis?.OverallConfidence,
                UserId = userId
            });
        }
        await _context.SaveChangesAsync();

        // ── Build response ───────────────────────────────────────────
        return Ok(new ScanResponse
        {
            Symbol = symbol,
            TotalPatterns = topPatterns.Count,
            CurrentPrice = price,
            DataSource = _marketData.DataSource,
            // Combined multi-timeframe info
            CombinedRegime = combinedRegime,
            CombinedRegimeConfidence = Math.Round(regimeConf, 1),
            RegimeAlignment = regimeAlign,
            DirectionAlignment = dirAlign,
            AlignmentScore = Math.Round(alignScore, 1),
            // Per-timeframe summaries
            TimeframeSummaries = timeframes.Select(t => new TimeframeSummaryDto
            {
                Timeframe = t.Timeframe,
                PatternCount = t.Patterns.Count,
                DominantDirection = t.DominantDirection,
                BullishCount = t.BullishCount,
                BearishCount = t.BearishCount,
                Regime = t.Context.Regime,
                RegimeConfidence = Math.Round(t.Context.RegimeConfidence, 1),
                RSI = t.Context.RSI.HasValue ? Math.Round(t.Context.RSI.Value, 1) : null,
                VolumeProfile = t.Context.VolumeProfile
            }).ToList(),
            // Context layers — all timeframes + Daily as default
            Context = MapContextDto(ctxDaily, "Daily", keyLevels),
            TimeframeContexts = new List<ContextDto>
            {
                MapContextDto(ctx1H, "1H", null),
                MapContextDto(ctx4H, "4H", null),
                MapContextDto(ctxDaily, "Daily", keyLevels),
                MapContextDto(ctxWeekly, "Weekly", null)
            },
            // Patterns (deduplicated top)
            Patterns = topPatterns.Select(p =>
            {
                var verdict = aiSynthesis?.PatternVerdicts.FirstOrDefault(v =>
                    v.PatternName.Equals(p.PatternName, StringComparison.OrdinalIgnoreCase));
                return new PatternDto
                {
                    PatternName = p.PatternName,
                    PatternType = p.PatternType.ToString(),
                    Direction = p.Direction.ToString(),
                    Timeframe = p.Timeframe,
                    Confidence = Math.Round(p.Confidence, 1),
                    HistoricalWinRate = p.HistoricalWinRate,
                    Description = p.Description,
                    // Prefer AI-recommended levels, fall back to detector's math
                    // Validate AI levels are within 20% of current price, otherwise use detector levels
                    SuggestedEntry = GetValidatedPrice(verdict?.Entry, p.SuggestedEntry, price),
                    SuggestedStop = GetValidatedPrice(verdict?.Stop, p.SuggestedStop, price),
                    SuggestedTarget = GetValidatedPrice(verdict?.Target, p.SuggestedTarget, price),
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    StartCandleIndex = p.StartIndex,
                    EndCandleIndex = p.EndIndex,
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
                // Validate AI recommended levels — use best pattern's detector levels as fallback
                RecommendedEntry = ValidateAILevel(aiSynthesis.RecommendedEntry, price, topPatterns.FirstOrDefault()?.SuggestedEntry),
                RecommendedStop = ValidateAILevel(aiSynthesis.RecommendedStop, price, topPatterns.FirstOrDefault()?.SuggestedStop),
                RecommendedTarget = ValidateAILevel(aiSynthesis.RecommendedTarget, price, topPatterns.FirstOrDefault()?.SuggestedTarget),
                RiskReward = aiSynthesis.RiskReward
            } : null,
            // Chart candle data — all three timeframes for client-side switching
            ChartCandles = candlesDaily.TakeLast(120).Select(c => new CandleDto
            {
                Time = new DateTimeOffset(c.Time).ToUnixTimeSeconds(),
                Open = Math.Round(c.Open, 2),
                High = Math.Round(c.High, 2),
                Low = Math.Round(c.Low, 2),
                Close = Math.Round(c.Close, 2),
                Volume = (long)c.Volume
            }).ToList(),
            ChartCandles1H = candles1H.TakeLast(120).Select(c => new CandleDto
            {
                Time = new DateTimeOffset(c.Time).ToUnixTimeSeconds(),
                Open = Math.Round(c.Open, 2),
                High = Math.Round(c.High, 2),
                Low = Math.Round(c.Low, 2),
                Close = Math.Round(c.Close, 2),
                Volume = (long)c.Volume
            }).ToList(),
            ChartCandles4H = candles4H.TakeLast(120).Select(c => new CandleDto
            {
                Time = new DateTimeOffset(c.Time).ToUnixTimeSeconds(),
                Open = Math.Round(c.Open, 2),
                High = Math.Round(c.High, 2),
                Low = Math.Round(c.Low, 2),
                Close = Math.Round(c.Close, 2),
                Volume = (long)c.Volume
            }).ToList(),
            ChartCandlesWeekly = candlesWeekly.TakeLast(104).Select(c => new CandleDto
            {
                Time = new DateTimeOffset(c.Time).ToUnixTimeSeconds(),
                Open = Math.Round(c.Open, 2),
                High = Math.Round(c.High, 2),
                Low = Math.Round(c.Low, 2),
                Close = Math.Round(c.Close, 2),
                Volume = (long)c.Volume
            }).ToList()
        });
    }

    // ═══════════════════════════════════════════════════════════════════
    // HISTORY
    // ═══════════════════════════════════════════════════════════════════

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

        var results = await query.Take(100).ToListAsync();
        return Ok(results.Select(p => new
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
        }));
    }

    // ═══════════════════════════════════════════════════════════════════
    // CONTEXT LAYER BUILDERS
    // ═══════════════════════════════════════════════════════════════════

    private MarketContext BuildMarketContext(List<Candle> candles, string timeframe)
    {
        var ctx = new MarketContext { Timeframe = timeframe };
        if (candles.Count < 20)
        {
            Console.WriteLine($"📊 BuildMarketContext {timeframe}: SKIPPED — only {candles.Count} candles (need 20+)");
            return ctx;
        }

        var closes = candles.Select(c => c.Close).ToList();
        var uniqueCloses = closes.TakeLast(20).Distinct().Count();
        Console.WriteLine($"📊 BuildMarketContext {timeframe}: {candles.Count} candles, " +
            $"last close: {candles.Last().Close:F6}, unique in last 20: {uniqueCloses}, " +
            $"vol: {candles.Last().Volume:F0}");
        var volumes = candles.Select(c => c.Volume).ToList();
        var last = candles.Last();
        ctx.CurrentPrice = last.Close;

        // Volume
        ctx.CurrentVolume = last.Volume;
        ctx.AvgVolume20 = volumes.TakeLast(20).Average();
        ctx.VolumeRatio = ctx.AvgVolume20 > 0 ? ctx.CurrentVolume / ctx.AvgVolume20 : 1;
        ctx.VolumeProfile = ctx.VolumeRatio switch
        {
            > 2.0m => "Breakout",
            > 1.3m => "High",
            > 0.7m => "Normal",
            _ => "Low"
        };

        // Moving averages
        if (closes.Count >= 20)
        {
            ctx.SMA20 = closes.TakeLast(20).Average();
            ctx.DistFromSMA20Pct = (last.Close - ctx.SMA20.Value) / ctx.SMA20.Value * 100;
        }
        if (closes.Count >= 50)
        {
            ctx.SMA50 = closes.TakeLast(50).Average();
            ctx.DistFromSMA50Pct = (last.Close - ctx.SMA50.Value) / ctx.SMA50.Value * 100;
        }
        if (closes.Count >= 200)
        {
            ctx.SMA200 = closes.TakeLast(200).Average();
            ctx.DistFromSMA200Pct = (last.Close - ctx.SMA200.Value) / ctx.SMA200.Value * 100;
        }

        // MA alignment
        if (ctx.SMA20.HasValue && ctx.SMA50.HasValue && ctx.SMA200.HasValue)
        {
            if (ctx.SMA20 > ctx.SMA50 && ctx.SMA50 > ctx.SMA200)
                ctx.MAAlignment = "Bullish Stack";
            else if (ctx.SMA20 < ctx.SMA50 && ctx.SMA50 < ctx.SMA200)
                ctx.MAAlignment = "Bearish Stack";
            else
                ctx.MAAlignment = "Mixed";
        }
        else if (ctx.SMA20.HasValue && ctx.SMA50.HasValue)
        {
            ctx.MAAlignment = ctx.SMA20 > ctx.SMA50 ? "Bullish" : "Bearish";
        }

        // RSI (14-period)
        if (closes.Count >= 15)
        {
            decimal gains = 0, losses = 0;
            for (int i = closes.Count - 14; i < closes.Count; i++)
            {
                var diff = closes[i] - closes[i - 1];
                if (diff > 0) gains += diff; else losses -= diff;
            }
            var avgGain = gains / 14; var avgLoss = losses / 14;
            var rs = avgLoss == 0 ? 100 : avgGain / avgLoss;
            ctx.RSI = 100 - (100 / (1 + rs));
            ctx.RSIZone = ctx.RSI < 30 ? "Oversold" : ctx.RSI > 70 ? "Overbought" : "Neutral";
        }

        // ATR (14-period)
        if (candles.Count >= 15)
        {
            var atrSum = 0m;
            for (int i = candles.Count - 14; i < candles.Count; i++)
            {
                var tr = Math.Max(candles[i].High - candles[i].Low,
                    Math.Max(Math.Abs(candles[i].High - candles[i - 1].Close),
                             Math.Abs(candles[i].Low - candles[i - 1].Close)));
                atrSum += tr;
            }
            ctx.ATR = atrSum / 14;
            ctx.ATRPercent = ctx.ATR / last.Close * 100;
        }

        // Consecutive days
        for (int i = candles.Count - 1; i >= 1; i--)
        {
            if (candles[i].Close > candles[i - 1].Close) ctx.ConsecutiveUpDays++;
            else break;
        }
        for (int i = candles.Count - 1; i >= 1; i--)
        {
            if (candles[i].Close < candles[i - 1].Close) ctx.ConsecutiveDownDays++;
            else break;
        }

        // Trend slope (SMA20 slope over last 5 periods)
        if (closes.Count >= 25)
        {
            var sma20Now = closes.TakeLast(20).Average();
            var sma20Prev = closes.Skip(closes.Count - 25).Take(20).Average();
            ctx.TrendSlope20 = (sma20Now - sma20Prev) / sma20Prev * 100;
        }

        // Simple regime classification
        var rsi = ctx.RSI ?? 50;
        var atrPct = ctx.ATRPercent ?? 1;
        if (ctx.TrendSlope20.HasValue && Math.Abs(ctx.TrendSlope20.Value) > 2 && rsi > 40 && rsi < 60)
        {
            ctx.Regime = "Trending";
            ctx.RegimeConfidence = 70 + Math.Min(Math.Abs(ctx.TrendSlope20.Value) * 3, 25);
        }
        else if (atrPct > 3)
        {
            ctx.Regime = "VolExpansion";
            ctx.RegimeConfidence = 65 + Math.Min(atrPct * 3, 30);
        }
        else if (rsi > 35 && rsi < 65 && atrPct < 1.5m)
        {
            ctx.Regime = "MeanReversion";
            ctx.RegimeConfidence = 60;
        }
        else
        {
            ctx.Regime = "Choppy";
            ctx.RegimeConfidence = 55;
        }

        return ctx;
    }

    private List<KeyPriceLevel> DetectKeyLevels(List<Candle> candles)
    {
        var levels = new List<KeyPriceLevel>();
        if (candles.Count < 30) return levels;

        var price = candles.Last().Close;

        // Round numbers
        var roundStep = price switch
        {
            > 500 => 50m,
            > 100 => 25m,
            > 50 => 10m,
            > 10 => 5m,
            _ => 1m
        };
        var baseRound = Math.Floor(price / roundStep) * roundStep;
        for (var r = baseRound - roundStep * 2; r <= baseRound + roundStep * 3; r += roundStep)
        {
            if (r <= 0) continue;
            levels.Add(new KeyPriceLevel
            {
                Price = r,
                Type = "Round Number",
                Description = $"Psychological level at {r:C0}"
            });
        }

        // Swing highs/lows (5-bar lookback)
        for (int i = 5; i < candles.Count - 5; i++)
        {
            bool isSwingHigh = true, isSwingLow = true;
            for (int j = 1; j <= 5; j++)
            {
                if (candles[i].High <= candles[i - j].High || candles[i].High <= candles[i + j].High) isSwingHigh = false;
                if (candles[i].Low >= candles[i - j].Low || candles[i].Low >= candles[i + j].Low) isSwingLow = false;
            }

            if (isSwingHigh)
            {
                var existing = levels.FirstOrDefault(l => Math.Abs(l.Price - candles[i].High) / price < 0.01m);
                if (existing != null) existing.TouchCount++;
                else levels.Add(new KeyPriceLevel
                {
                    Price = candles[i].High,
                    Type = "Resistance",
                    TouchCount = 1,
                    Description = $"Swing high from {candles[i].Time:MMM dd}"
                });
            }
            if (isSwingLow)
            {
                var existing = levels.FirstOrDefault(l => Math.Abs(l.Price - candles[i].Low) / price < 0.01m);
                if (existing != null) existing.TouchCount++;
                else levels.Add(new KeyPriceLevel
                {
                    Price = candles[i].Low,
                    Type = "Support",
                    TouchCount = 1,
                    Description = $"Swing low from {candles[i].Time:MMM dd}"
                });
            }
        }

        return levels.OrderByDescending(l => l.TouchCount).ThenBy(l => Math.Abs(l.Price - price)).Take(10).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maps internal MarketContext to ContextDto for API response.
    /// Key levels are only attached to Daily (they apply cross-timeframe).
    /// </summary>
    private static ContextDto MapContextDto(MarketContext ctx, string timeframe, List<KeyPriceLevel>? keyLevels)
    {
        var decimals = ctx.CurrentPrice < 1m ? 6 : ctx.CurrentPrice < 10m ? 4 : 2;
        return new ContextDto
        {
            Timeframe = timeframe,
            VolumeRatio = Math.Round(ctx.VolumeRatio, 1),
            VolumeProfile = ctx.VolumeProfile,
            NearestSupport = ctx.NearestSupport.HasValue ? Math.Round(ctx.NearestSupport.Value, decimals) : null,
            NearestResistance = ctx.NearestResistance.HasValue ? Math.Round(ctx.NearestResistance.Value, decimals) : null,
            DistToSupportPct = ctx.DistanceToSupportPct.HasValue ? Math.Round(ctx.DistanceToSupportPct.Value, 1) : null,
            DistToResistancePct = ctx.DistanceToResistancePct.HasValue ? Math.Round(ctx.DistanceToResistancePct.Value, 1) : null,
            SMA20 = ctx.SMA20.HasValue ? Math.Round(ctx.SMA20.Value, decimals) : null,
            SMA50 = ctx.SMA50.HasValue ? Math.Round(ctx.SMA50.Value, decimals) : null,
            SMA200 = ctx.SMA200.HasValue ? Math.Round(ctx.SMA200.Value, decimals) : null,
            DistFromSMA200Pct = ctx.DistFromSMA200Pct.HasValue ? Math.Round(ctx.DistFromSMA200Pct.Value, 1) : null,
            MAAlignment = ctx.MAAlignment,
            RSI = ctx.RSI.HasValue ? Math.Round(ctx.RSI.Value, 1) : null,
            RSIZone = ctx.RSIZone,
            ATRPercent = ctx.ATRPercent.HasValue ? Math.Round(ctx.ATRPercent.Value, 2) : null,
            ConsecutiveUpDays = ctx.ConsecutiveUpDays,
            ConsecutiveDownDays = ctx.ConsecutiveDownDays,
            KeyLevels = keyLevels?.Take(6).Select(l => new KeyLevelDto
            {
                Price = Math.Round(l.Price, decimals),
                Type = l.Type,
                TouchCount = l.TouchCount
            }).ToList() ?? new()
        };
    }

    /// <summary>
    /// Validates an AI-suggested price level against current price. Returns detector fallback if AI hallucinated.
    /// Uses appropriate decimal precision for low-price assets.
    /// </summary>
    private static decimal GetValidatedPrice(decimal? aiPrice, decimal detectorPrice, decimal currentPrice)
    {
        var maxDrift = currentPrice * 0.20m; // 20% tolerance
        var decimals = currentPrice < 1m ? 6 : currentPrice < 10m ? 4 : 2;

        if (aiPrice.HasValue && aiPrice.Value > 0 && Math.Abs(aiPrice.Value - currentPrice) <= maxDrift)
            return Math.Round(aiPrice.Value, decimals);

        return Math.Round(detectorPrice, decimals);
    }

    /// <summary>
    /// Validates AI recommended entry/stop/target for the overall analysis. Falls back to detector price.
    /// </summary>
    private static decimal? ValidateAILevel(decimal? aiLevel, decimal currentPrice, decimal? detectorFallback)
    {
        var maxDrift = currentPrice * 0.20m;
        var decimals = currentPrice < 1m ? 6 : currentPrice < 10m ? 4 : 2;

        if (aiLevel.HasValue && aiLevel.Value > 0 && Math.Abs(aiLevel.Value - currentPrice) <= maxDrift)
            return Math.Round(aiLevel.Value, decimals);

        if (detectorFallback.HasValue && detectorFallback.Value > 0)
            return Math.Round(detectorFallback.Value, decimals);

        return null;
    }

    private List<PatternResult> DeduplicatePatterns(List<PatternResult> patterns)
    {
        return patterns
            .GroupBy(p => p.PatternName + "|" + p.Direction)
            .Select(g => g.OrderByDescending(p => p.Confidence).First())
            .OrderByDescending(p => p.Confidence)
            .ToList();
    }

    private TimeframeData BuildTimeframeData(string tf, decimal weight, List<PatternResult> patterns, MarketContext ctx)
    {
        var bullish = patterns.Count(p => p.Direction == PatternDirection.Bullish);
        var bearish = patterns.Count(p => p.Direction == PatternDirection.Bearish);
        var neutral = patterns.Count(p => p.Direction == PatternDirection.Neutral);

        return new TimeframeData
        {
            Timeframe = tf,
            Weight = weight,
            Patterns = patterns,
            Context = ctx,
            BullishCount = bullish,
            BearishCount = bearish,
            NeutralCount = neutral,
            DominantDirection = bullish > bearish ? "Bullish" : bearish > bullish ? "Bearish" : "Neutral"
        };
    }

    private (string regime, decimal confidence, string alignment) CombineRegimes(List<TimeframeData> timeframes)
    {
        // Weighted vote: Weekly 3x, Daily 2x, 1H 1x
        var regimeVotes = new Dictionary<string, decimal>();
        var totalWeight = 0m;

        foreach (var tf in timeframes)
        {
            var regime = tf.Context.Regime;
            if (string.IsNullOrEmpty(regime)) continue;
            if (!regimeVotes.ContainsKey(regime)) regimeVotes[regime] = 0;
            regimeVotes[regime] += tf.Weight * tf.Context.RegimeConfidence;
            totalWeight += tf.Weight * 100;
        }

        if (!regimeVotes.Any()) return ("Choppy", 50, "Unknown");

        var winner = regimeVotes.OrderByDescending(kv => kv.Value).First();
        var confidence = totalWeight > 0 ? winner.Value / totalWeight * 100 : 50;

        // Check alignment
        var regimes = timeframes.Select(t => t.Context.Regime).Where(r => !string.IsNullOrEmpty(r)).Distinct().ToList();
        var alignment = regimes.Count switch
        {
            1 => "Aligned",
            2 => "Mixed",
            _ => "Conflicting"
        };

        return (winner.Key, confidence, alignment);
    }

    private (string alignment, decimal score) CalculateDirectionAlignment(List<TimeframeData> timeframes)
    {
        // Weighted direction score: Weekly 3x, Daily 2x, 1H 1x
        var bullishScore = 0m;
        var bearishScore = 0m;

        foreach (var tf in timeframes)
        {
            bullishScore += tf.BullishCount * tf.Weight;
            bearishScore += tf.BearishCount * tf.Weight;
        }

        var total = bullishScore + bearishScore;
        if (total == 0) return ("Neutral", 50);

        var dominantPct = Math.Max(bullishScore, bearishScore) / total * 100;

        var directions = timeframes.Select(t => t.DominantDirection).Where(d => d != "Neutral").Distinct().ToList();
        string alignment;
        if (directions.Count == 1)
            alignment = directions[0] == "Bullish" ? "All Bullish" : "All Bearish";
        else if (directions.Count == 0)
            alignment = "Neutral";
        else
            alignment = "Conflicting";

        return (alignment, dominantPct);
    }

}

// ═══════════════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════════════

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
    public string DataSource { get; set; } = ""; // "Alpaca" or "Sample Data"

    // Multi-timeframe
    public string CombinedRegime { get; set; } = "";
    public decimal CombinedRegimeConfidence { get; set; }
    public string RegimeAlignment { get; set; } = "";
    public string DirectionAlignment { get; set; } = "";
    public decimal AlignmentScore { get; set; }
    public List<TimeframeSummaryDto> TimeframeSummaries { get; set; } = new();

    // Context
    public ContextDto? Context { get; set; }
    public List<ContextDto> TimeframeContexts { get; set; } = new();

    // Patterns + AI
    public List<PatternDto> Patterns { get; set; } = new();
    public AIAnalysisDto? AIAnalysis { get; set; }

    // Chart data (last 120 daily candles for pattern overlay chart)
    public List<CandleDto> ChartCandles { get; set; } = new();
    public List<CandleDto> ChartCandles1H { get; set; } = new();
    public List<CandleDto> ChartCandles4H { get; set; } = new();
    public List<CandleDto> ChartCandlesWeekly { get; set; } = new();
}

public class CandleDto
{
    public long Time { get; set; }   // Unix timestamp
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

public class TimeframeSummaryDto
{
    public string Timeframe { get; set; } = "";
    public int PatternCount { get; set; }
    public string DominantDirection { get; set; } = "";
    public int BullishCount { get; set; }
    public int BearishCount { get; set; }
    public string Regime { get; set; } = "";
    public decimal RegimeConfidence { get; set; }
    public decimal? RSI { get; set; }
    public string VolumeProfile { get; set; } = "";
}

public class ContextDto
{
    public string Timeframe { get; set; } = "Daily";
    public decimal VolumeRatio { get; set; }
    public string VolumeProfile { get; set; } = "";
    public decimal? NearestSupport { get; set; }
    public decimal? NearestResistance { get; set; }
    public decimal? DistToSupportPct { get; set; }
    public decimal? DistToResistancePct { get; set; }
    public decimal? SMA20 { get; set; }
    public decimal? SMA50 { get; set; }
    public decimal? SMA200 { get; set; }
    public decimal? DistFromSMA200Pct { get; set; }
    public string MAAlignment { get; set; } = "";
    public decimal? RSI { get; set; }
    public string RSIZone { get; set; } = "";
    public decimal? ATRPercent { get; set; }
    public int ConsecutiveUpDays { get; set; }
    public int ConsecutiveDownDays { get; set; }
    public List<KeyLevelDto> KeyLevels { get; set; } = new();
}

public class KeyLevelDto
{
    public decimal Price { get; set; }
    public string Type { get; set; } = "";
    public int TouchCount { get; set; }
}

public class PatternDto
{
    public string PatternName { get; set; } = "";
    public string PatternType { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public decimal Confidence { get; set; }
    public decimal HistoricalWinRate { get; set; }
    public string Description { get; set; } = "";
    public decimal SuggestedEntry { get; set; }
    public decimal SuggestedStop { get; set; }
    public decimal SuggestedTarget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int StartCandleIndex { get; set; }
    public int EndCandleIndex { get; set; }
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