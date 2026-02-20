using Amplify.Application.Common.Interfaces.AI;
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

    public PatternScannerController(IPatternDetector detector, IPatternAnalyzer analyzer, ApplicationDbContext context, TradeSimulationService simulation)
    {
        _detector = detector;
        _analyzer = analyzer;
        _context = context;
        _simulation = simulation;
    }

    // ═══════════════════════════════════════════════════════════════════
    // MULTI-TIMEFRAME SCAN
    // ═══════════════════════════════════════════════════════════════════

    [HttpPost("scan")]
    public async Task<IActionResult> ScanSymbol([FromBody] ScanRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var symbol = request.Symbol.ToUpper();

        // ── Generate candles for each timeframe ──────────────────────
        var candles4H = GenerateSampleCandles(symbol, 200, "4H");
        var candlesDaily = GenerateSampleCandles(symbol, 250, "Daily");
        var candlesWeekly = GenerateSampleCandles(symbol, 104, "Weekly"); // ~2 years

        // ── Detect patterns per timeframe ────────────────────────────
        var patterns4H = _detector.DetectAll(candles4H);
        patterns4H.ForEach(p => p.Timeframe = "4H");

        var patternsDaily = _detector.DetectAll(candlesDaily);
        patternsDaily.ForEach(p => p.Timeframe = "Daily");

        var patternsWeekly = _detector.DetectAll(candlesWeekly);
        patternsWeekly.ForEach(p => p.Timeframe = "Weekly");

        // ── Build context per timeframe ──────────────────────────────
        var ctx4H = BuildMarketContext(candles4H, "4H");
        var ctxDaily = BuildMarketContext(candlesDaily, "Daily");
        var ctxWeekly = BuildMarketContext(candlesWeekly, "Weekly");

        // ── Deduplicate patterns per timeframe ───────────────────────
        var dedup4H = DeduplicatePatterns(patterns4H);
        var dedupDaily = DeduplicatePatterns(patternsDaily);
        var dedupWeekly = DeduplicatePatterns(patternsWeekly);

        // ── Apply timeframe weights to confidence ────────────────────
        // Weekly patterns get boosted, 4H get reduced
        foreach (var p in dedup4H) p.Confidence = Math.Min(p.Confidence * 0.8m, 100);
        foreach (var p in dedupWeekly) p.Confidence = Math.Min(p.Confidence * 1.2m, 100);

        // ── Build timeframe data ─────────────────────────────────────
        var tf4H = BuildTimeframeData("4H", 1.0m, dedup4H, ctx4H);
        var tfDaily = BuildTimeframeData("Daily", 2.0m, dedupDaily, ctxDaily);
        var tfWeekly = BuildTimeframeData("Weekly", 3.0m, dedupWeekly, ctxWeekly);

        var timeframes = new List<TimeframeData> { tf4H, tfDaily, tfWeekly };

        // ── Combined regime (weighted) ───────────────────────────────
        var (combinedRegime, regimeConf, regimeAlign) = CombineRegimes(timeframes);

        // ── Direction alignment ──────────────────────────────────────
        var (dirAlign, alignScore) = CalculateDirectionAlignment(timeframes);

        // ── Top patterns (best per type across all timeframes) ───────
        var allPatterns = dedup4H.Concat(dedupDaily).Concat(dedupWeekly).ToList();
        var topPatterns = allPatterns
            .GroupBy(p => p.PatternName)
            .Select(g => g.OrderByDescending(p => p.Confidence).First())
            .OrderByDescending(p => p.Confidence)
            .Take(12)
            .ToList();

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
                    topPatterns, timeframes, ctxDaily, combinedRegime, regimeConf, dirAlign, alignScore, symbol,
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
            // Context layers
            Context = new ContextDto
            {
                VolumeRatio = Math.Round(ctxDaily.VolumeRatio, 1),
                VolumeProfile = ctxDaily.VolumeProfile,
                NearestSupport = ctxDaily.NearestSupport.HasValue ? Math.Round(ctxDaily.NearestSupport.Value, 2) : null,
                NearestResistance = ctxDaily.NearestResistance.HasValue ? Math.Round(ctxDaily.NearestResistance.Value, 2) : null,
                DistToSupportPct = ctxDaily.DistanceToSupportPct.HasValue ? Math.Round(ctxDaily.DistanceToSupportPct.Value, 1) : null,
                DistToResistancePct = ctxDaily.DistanceToResistancePct.HasValue ? Math.Round(ctxDaily.DistanceToResistancePct.Value, 1) : null,
                SMA20 = ctxDaily.SMA20.HasValue ? Math.Round(ctxDaily.SMA20.Value, 2) : null,
                SMA50 = ctxDaily.SMA50.HasValue ? Math.Round(ctxDaily.SMA50.Value, 2) : null,
                SMA200 = ctxDaily.SMA200.HasValue ? Math.Round(ctxDaily.SMA200.Value, 2) : null,
                DistFromSMA200Pct = ctxDaily.DistFromSMA200Pct.HasValue ? Math.Round(ctxDaily.DistFromSMA200Pct.Value, 1) : null,
                MAAlignment = ctxDaily.MAAlignment,
                RSI = ctxDaily.RSI.HasValue ? Math.Round(ctxDaily.RSI.Value, 1) : null,
                RSIZone = ctxDaily.RSIZone,
                ATRPercent = ctxDaily.ATRPercent.HasValue ? Math.Round(ctxDaily.ATRPercent.Value, 2) : null,
                ConsecutiveUpDays = ctxDaily.ConsecutiveUpDays,
                ConsecutiveDownDays = ctxDaily.ConsecutiveDownDays,
                KeyLevels = keyLevels.Take(6).Select(l => new KeyLevelDto
                {
                    Price = Math.Round(l.Price, 2),
                    Type = l.Type,
                    TouchCount = l.TouchCount
                }).ToList()
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
        if (candles.Count < 20) return ctx;

        var closes = candles.Select(c => c.Close).ToList();
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
        // Weighted vote: Weekly 3x, Daily 2x, 4H 1x
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
        // Weighted direction score: Weekly 3x, Daily 2x, 4H 1x
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

    // ═══════════════════════════════════════════════════════════════════
    // SAMPLE CANDLE GENERATION
    // ═══════════════════════════════════════════════════════════════════

    private List<Candle> GenerateSampleCandles(string symbol, int count, string timeframe)
    {
        var candles = new List<Candle>();
        var random = new Random(symbol.GetHashCode() + timeframe.GetHashCode());

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
            "COIN" => 250m,
            _ => 150m
        };

        // Volatility scale by timeframe
        var volScale = timeframe switch
        {
            "4H" => 0.008m,
            "Weekly" => 0.035m,
            _ => 0.02m  // Daily
        };

        var price = basePrice * 0.85m;

        for (int i = count; i >= 0; i--)
        {
            DateTime date;
            if (timeframe == "4H")
            {
                date = DateTime.Today.AddHours(-i * 4);
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            }
            else if (timeframe == "Weekly")
            {
                date = DateTime.Today.AddDays(-i * 7);
            }
            else
            {
                date = DateTime.Today.AddDays(-i);
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            }

            var change = (decimal)(random.NextDouble() - 0.47) * basePrice * volScale;
            var open = price;
            var close = price + change;
            var high = Math.Max(open, close) + (decimal)random.NextDouble() * basePrice * volScale * 0.4m;
            var low = Math.Min(open, close) - (decimal)random.NextDouble() * basePrice * volScale * 0.4m;
            var volume = (5000000m + (decimal)random.Next(0, 15000000)) * (timeframe == "Weekly" ? 5 : timeframe == "4H" ? 0.25m : 1);

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

    // Multi-timeframe
    public string CombinedRegime { get; set; } = "";
    public decimal CombinedRegimeConfidence { get; set; }
    public string RegimeAlignment { get; set; } = "";
    public string DirectionAlignment { get; set; } = "";
    public decimal AlignmentScore { get; set; }
    public List<TimeframeSummaryDto> TimeframeSummaries { get; set; } = new();

    // Context
    public ContextDto? Context { get; set; }

    // Patterns + AI
    public List<PatternDto> Patterns { get; set; } = new();
    public AIAnalysisDto? AIAnalysis { get; set; }
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