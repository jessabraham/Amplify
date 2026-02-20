using Amplify.Application.Common.Interfaces.AI;
using Amplify.Application.Common.Models;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace Amplify.Infrastructure.ExternalServices.AI;

public class OllamaPatternAnalyzer : IPatternAnalyzer
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly string _baseUrl;

    public OllamaPatternAnalyzer(HttpClient http, IConfiguration config)
    {
        _http = http;
        _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model = config["Ollama:Model"] ?? "qwen3:8b";
    }

    public async Task<PatternAnalysis> AnalyzePatternAsync(PatternResult pattern, List<Candle> recentCandles, string symbol)
    {
        var priceContext = BuildPriceContext(recentCandles);

        var prompt = $@"You are an expert technical analyst. Analyze this detected chart pattern and provide your assessment.

SYMBOL: {symbol}
CURRENT PRICE: {recentCandles.Last().Close:F2}

DETECTED PATTERN: {pattern.PatternName}
DIRECTION: {pattern.Direction}
MATH CONFIDENCE: {pattern.Confidence:F0}%
HISTORICAL WIN RATE: {pattern.HistoricalWinRate}%
DESCRIPTION: {pattern.Description}

SUGGESTED LEVELS:
- Entry: {pattern.SuggestedEntry:F2}
- Stop Loss: {pattern.SuggestedStop:F2}
- Target: {pattern.SuggestedTarget:F2}

RECENT PRICE ACTION (last 10 candles):
{priceContext}

Respond in this EXACT JSON format (no markdown, no code blocks, just raw JSON):
{{
  ""aiConfidence"": <number 0-100>,
  ""isValid"": <true/false>,
  ""grade"": ""<A+/A/B+/B/C/D/F>"",
  ""reasoning"": ""<2-3 sentences on why you agree or disagree with this pattern detection>"",
  ""riskAssessment"": ""<1-2 sentences on what could invalidate this pattern>"",
  ""tradePlan"": ""<2-3 sentences: specific entry trigger, position sizing suggestion, exit strategy>""
}}";

        try
        {
            var response = await CallOllama(prompt);
            return ParsePatternAnalysis(response);
        }
        catch
        {
            return new PatternAnalysis
            {
                AIConfidence = pattern.Confidence * 0.8m,
                IsValid = pattern.Confidence >= 65,
                Grade = pattern.Confidence >= 75 ? "B" : "C",
                Reasoning = "AI analysis unavailable — using math-only confidence.",
                RiskAssessment = "Unable to assess risk without AI. Use caution.",
                TradePlan = $"Entry at {pattern.SuggestedEntry:F2}, stop at {pattern.SuggestedStop:F2}, target at {pattern.SuggestedTarget:F2}."
            };
        }
    }

    public async Task<MultiPatternAnalysis> SynthesizePatternsAsync(List<PatternResult> patterns, List<Candle> recentCandles, string symbol)
    {
        var priceContext = BuildPriceContext(recentCandles);
        var currentPrice = recentCandles.Last().Close;

        var patternList = new StringBuilder();
        foreach (var p in patterns)
        {
            patternList.AppendLine($"- {p.PatternName} ({p.Direction}, Confidence: {p.Confidence:F0}%, Win Rate: {p.HistoricalWinRate}%)");
            patternList.AppendLine($"  Entry: {p.SuggestedEntry:F2} | Stop: {p.SuggestedStop:F2} | Target: {p.SuggestedTarget:F2}");
        }

        var bullishCount = patterns.Count(p => p.Direction == Domain.Enumerations.PatternDirection.Bullish);
        var bearishCount = patterns.Count(p => p.Direction == Domain.Enumerations.PatternDirection.Bearish);

        var prompt = $@"You are a senior technical analyst. Multiple patterns have been detected on {symbol}. Synthesize them into a unified trading recommendation.

SYMBOL: {symbol}
CURRENT PRICE: {currentPrice:F2}
TOTAL PATTERNS FOUND: {patterns.Count}
BULLISH PATTERNS: {bullishCount}
BEARISH PATTERNS: {bearishCount}

DETECTED PATTERNS:
{patternList}

RECENT PRICE ACTION (last 10 candles):
{priceContext}

Analyze ALL patterns together. Consider:
1. Do the patterns confirm or contradict each other?
2. Which patterns are strongest and most reliable?
3. What is the overall market bias?
4. What is the best trade setup considering all signals?

Respond in this EXACT JSON format (no markdown, no code blocks, just raw JSON):
{{
  ""overallBias"": ""<Strong Bullish/Bullish/Neutral/Bearish/Strong Bearish>"",
  ""overallConfidence"": <number 0-100>,
  ""summary"": ""<2-3 sentence synthesis of all patterns>"",
  ""detailedAnalysis"": ""<4-6 sentences: deeper analysis of pattern confluence, key levels, volume, momentum>"",
  ""recommendedAction"": ""<Buy/Sell/Wait/Watch>"",
  ""recommendedEntry"": <price or null>,
  ""recommendedStop"": <price or null>,
  ""recommendedTarget"": <price or null>,
  ""riskReward"": ""<e.g. 1:2.5>"",
  ""patternVerdicts"": [
    {{
      ""patternName"": ""<name>"",
      ""isValid"": <true/false>,
      ""grade"": ""<A+/A/B+/B/C/D/F>"",
      ""oneLineReason"": ""<brief reason>""
    }}
  ]
}}";

        try
        {
            var response = await CallOllama(prompt);
            var analysis = ParseMultiPatternAnalysis(response);
            analysis.Symbol = symbol;
            return analysis;
        }
        catch
        {
            return new MultiPatternAnalysis
            {
                Symbol = symbol,
                OverallBias = bullishCount > bearishCount ? "Bullish" : bearishCount > bullishCount ? "Bearish" : "Neutral",
                OverallConfidence = patterns.Any() ? patterns.Average(p => p.Confidence) : 0,
                Summary = "AI synthesis unavailable — showing math-only analysis.",
                DetailedAnalysis = $"Found {bullishCount} bullish and {bearishCount} bearish patterns. Manual review recommended.",
                RecommendedAction = bullishCount > bearishCount ? "Watch" : "Wait",
                RiskReward = "N/A"
            };
        }
    }

    private string BuildPriceContext(List<Candle> candles)
    {
        var recent = candles.TakeLast(10).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("Date       | Open     | High     | Low      | Close    | Volume");
        sb.AppendLine("-----------|----------|----------|----------|----------|----------");
        foreach (var c in recent)
        {
            var dir = c.IsBullish ? "▲" : "▼";
            sb.AppendLine($"{c.Time:yyyy-MM-dd} | {c.Open,8:F2} | {c.High,8:F2} | {c.Low,8:F2} | {c.Close,8:F2} | {c.Volume,8:N0} {dir}");
        }
        return sb.ToString();
    }

    private async Task<string> CallOllama(string prompt)
    {
        var request = new
        {
            model = _model,
            prompt = prompt,
            stream = false,
            options = new { temperature = 0.3, num_predict = 2048 }
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"{_baseUrl}/api/generate", content);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("response").GetString() ?? "";
    }

    private PatternAnalysis ParsePatternAnalysis(string response)
    {
        try
        {
            // Extract JSON from response (AI might add extra text)
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0) throw new Exception("No JSON found");

            var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            return new PatternAnalysis
            {
                AIConfidence = root.TryGetProperty("aiConfidence", out var conf) ? conf.GetDecimal() : 50,
                IsValid = root.TryGetProperty("isValid", out var valid) && valid.GetBoolean(),
                Grade = root.TryGetProperty("grade", out var grade) ? grade.GetString() ?? "C" : "C",
                Reasoning = root.TryGetProperty("reasoning", out var reason) ? reason.GetString() ?? "" : "",
                RiskAssessment = root.TryGetProperty("riskAssessment", out var risk) ? risk.GetString() ?? "" : "",
                TradePlan = root.TryGetProperty("tradePlan", out var plan) ? plan.GetString() ?? "" : ""
            };
        }
        catch
        {
            return new PatternAnalysis
            {
                AIConfidence = 50,
                IsValid = true,
                Grade = "C",
                Reasoning = response.Length > 200 ? response[..200] : response,
                RiskAssessment = "Could not parse structured response.",
                TradePlan = "Manual review recommended."
            };
        }
    }

    private MultiPatternAnalysis ParseMultiPatternAnalysis(string response)
    {
        try
        {
            var jsonStart = response.IndexOf('{');
            var jsonEnd = response.LastIndexOf('}');
            if (jsonStart < 0 || jsonEnd < 0) throw new Exception("No JSON found");

            var jsonStr = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            var analysis = new MultiPatternAnalysis
            {
                OverallBias = root.TryGetProperty("overallBias", out var bias) ? bias.GetString() ?? "Neutral" : "Neutral",
                OverallConfidence = root.TryGetProperty("overallConfidence", out var conf) ? conf.GetDecimal() : 50,
                Summary = root.TryGetProperty("summary", out var sum) ? sum.GetString() ?? "" : "",
                DetailedAnalysis = root.TryGetProperty("detailedAnalysis", out var detail) ? detail.GetString() ?? "" : "",
                RecommendedAction = root.TryGetProperty("recommendedAction", out var action) ? action.GetString() ?? "Wait" : "Wait",
                RiskReward = root.TryGetProperty("riskReward", out var rr) ? rr.GetString() ?? "N/A" : "N/A"
            };

            if (root.TryGetProperty("recommendedEntry", out var entry) && entry.ValueKind == JsonValueKind.Number)
                analysis.RecommendedEntry = entry.GetDecimal();
            if (root.TryGetProperty("recommendedStop", out var stop) && stop.ValueKind == JsonValueKind.Number)
                analysis.RecommendedStop = stop.GetDecimal();
            if (root.TryGetProperty("recommendedTarget", out var target) && target.ValueKind == JsonValueKind.Number)
                analysis.RecommendedTarget = target.GetDecimal();

            if (root.TryGetProperty("patternVerdicts", out var verdicts) && verdicts.ValueKind == JsonValueKind.Array)
            {
                foreach (var v in verdicts.EnumerateArray())
                {
                    analysis.PatternVerdicts.Add(new PatternVerdict
                    {
                        PatternName = v.TryGetProperty("patternName", out var pn) ? pn.GetString() ?? "" : "",
                        IsValid = v.TryGetProperty("isValid", out var iv) && iv.GetBoolean(),
                        Grade = v.TryGetProperty("grade", out var g) ? g.GetString() ?? "C" : "C",
                        OneLineReason = v.TryGetProperty("oneLineReason", out var olr) ? olr.GetString() ?? "" : ""
                    });
                }
            }

            return analysis;
        }
        catch
        {
            return new MultiPatternAnalysis
            {
                OverallBias = "Neutral",
                OverallConfidence = 50,
                Summary = response.Length > 300 ? response[..300] : response,
                DetailedAnalysis = "Could not parse structured AI response.",
                RecommendedAction = "Wait"
            };
        }
    }

    public async Task<MultiPatternAnalysis> SynthesizeMultiTimeframeAsync(
        List<PatternResult> topPatterns,
        List<TimeframeData> timeframes,
        MarketContext dailyContext,
        string combinedRegime,
        decimal regimeConfidence,
        string directionAlignment,
        decimal alignmentScore,
        string symbol,
        List<PatternPerformanceData>? performanceStats = null,
        UserStatsData? userStats = null)
    {
        var currentPrice = dailyContext.CurrentPrice;

        // Build timeframe breakdown
        var tfBreakdown = new StringBuilder();
        foreach (var tf in timeframes)
        {
            tfBreakdown.AppendLine($"\n--- {tf.Timeframe} (weight: {tf.Weight}x) ---");
            tfBreakdown.AppendLine($"Regime: {tf.Context.Regime} ({tf.Context.RegimeConfidence:F0}% confidence)");
            tfBreakdown.AppendLine($"Direction: {tf.DominantDirection} ({tf.BullishCount} bullish, {tf.BearishCount} bearish)");
            if (tf.Context.RSI.HasValue) tfBreakdown.AppendLine($"RSI: {tf.Context.RSI:F1} ({tf.Context.RSIZone})");
            tfBreakdown.AppendLine($"Volume: {tf.Context.VolumeProfile} ({tf.Context.VolumeRatio:F1}x avg)");
            foreach (var p in tf.Patterns.Take(5))
                tfBreakdown.AppendLine($"  • {p.PatternName} ({p.Direction}, {p.Confidence:F0}%) — Entry: {p.SuggestedEntry:F2}, Stop: {p.SuggestedStop:F2}, Target: {p.SuggestedTarget:F2}");
        }

        // Build context section
        var contextSection = new StringBuilder();
        contextSection.AppendLine($"Volume: {dailyContext.VolumeProfile} ({dailyContext.VolumeRatio:F1}x average)");
        contextSection.AppendLine($"MA Alignment: {dailyContext.MAAlignment}");
        if (dailyContext.SMA20.HasValue) contextSection.AppendLine($"SMA20: {dailyContext.SMA20:F2} (price {dailyContext.DistFromSMA20Pct:+0.0;-0.0}% from SMA20)");
        if (dailyContext.SMA50.HasValue) contextSection.AppendLine($"SMA50: {dailyContext.SMA50:F2} (price {dailyContext.DistFromSMA50Pct:+0.0;-0.0}% from SMA50)");
        if (dailyContext.SMA200.HasValue) contextSection.AppendLine($"SMA200: {dailyContext.SMA200:F2} (price {dailyContext.DistFromSMA200Pct:+0.0;-0.0}% from SMA200)");
        if (dailyContext.RSI.HasValue) contextSection.AppendLine($"RSI(14): {dailyContext.RSI:F1} — {dailyContext.RSIZone}");
        if (dailyContext.ATRPercent.HasValue) contextSection.AppendLine($"ATR: {dailyContext.ATRPercent:F2}% of price");
        if (dailyContext.NearestSupport.HasValue) contextSection.AppendLine($"Nearest Support: {dailyContext.NearestSupport:F2} ({dailyContext.DistanceToSupportPct:F1}% below)");
        if (dailyContext.NearestResistance.HasValue) contextSection.AppendLine($"Nearest Resistance: {dailyContext.NearestResistance:F2} ({dailyContext.DistanceToResistancePct:F1}% above)");
        if (dailyContext.ConsecutiveUpDays > 0) contextSection.AppendLine($"Streak: {dailyContext.ConsecutiveUpDays} consecutive up days");
        if (dailyContext.ConsecutiveDownDays > 0) contextSection.AppendLine($"Streak: {dailyContext.ConsecutiveDownDays} consecutive down days");

        // Key levels
        var levelsSection = new StringBuilder();
        foreach (var kl in dailyContext.KeyLevels.Take(6))
            levelsSection.AppendLine($"  {kl.Type}: {kl.Price:F2} (touched {kl.TouchCount}x) — {kl.Description}");

        // ═══ PERFORMANCE FEEDBACK SECTION ═══
        var performanceSection = new StringBuilder();
        if (userStats is not null && userStats.TotalTrades >= 5)
        {
            performanceSection.AppendLine($"\n═══ YOUR TRADING HISTORY ({userStats.TotalTrades} trades) ═══");
            performanceSection.AppendLine($"Overall Win Rate: {userStats.WinRate:F1}% | Avg R-Multiple: {userStats.AvgRMultiple:F2}R");
            performanceSection.AppendLine($"Long Win Rate: {userStats.LongWinRate:F1}% | Short Win Rate: {userStats.ShortWinRate:F1}%");
            if (userStats.AlignedWinRate > 0)
                performanceSection.AppendLine($"Win Rate when TFs aligned: {userStats.AlignedWinRate:F1}% | When conflicting: {userStats.ConflictingWinRate:F1}%");
        }

        if (performanceStats is not null && performanceStats.Any())
        {
            performanceSection.AppendLine($"\n═══ PATTERN-SPECIFIC HISTORY ═══");
            foreach (var ps in performanceStats.OrderByDescending(p => p.TotalTrades).Take(8))
            {
                performanceSection.AppendLine($"  {ps.PatternName} ({ps.Direction}) on {ps.Timeframe} in {ps.Regime}:");
                performanceSection.AppendLine($"    {ps.Wins}W/{ps.Losses}L ({ps.WinRate:F0}% win rate) | Avg {ps.AvgRMultiple:F1}R | PF: {ps.ProfitFactor:F1} | Avg {ps.AvgDaysHeld:F0} days");
                if (ps.TradesWhenAligned > 0)
                    performanceSection.AppendLine($"    When TFs aligned: {ps.WinRateWhenAligned:F0}% ({ps.TradesWhenAligned} trades) | When conflicting: {ps.WinRateWhenConflicting:F0}% ({ps.TradesWhenConflicting} trades)");
                if (ps.TradesWithBreakoutVol > 0)
                    performanceSection.AppendLine($"    With breakout volume: {ps.WinRateWithBreakoutVol:F0}% ({ps.TradesWithBreakoutVol} trades)");
            }
        }

        var hasPerformance = performanceSection.Length > 0;

        var perfRules = hasPerformance ? @"
8. CRITICAL: Use the trader's ACTUAL HISTORY to adjust confidence. If their win rate on this specific pattern/timeframe/regime combo is low, WARN them explicitly and lower confidence.
9. If aligned TF win rate is much higher than conflicting, emphasize this in your recommendation.
10. If they have no history on this exact setup, say so — the confidence is based on general TA, not personal track record." : "";

        var perfSummaryHint = hasPerformance ? " and how it relates to your past performance" : "";
        var perfDetailHint = hasPerformance ? ", and personal track record insights" : "";
        var perfVerdictHint = hasPerformance ? " and personal win rate if available" : "";

        var prompt = $@"You are a senior technical analyst performing a MULTI-TIMEFRAME analysis on {symbol}.
Higher timeframes carry MORE weight. A weekly signal overrides conflicting daily/4H signals.
Timeframe alignment (all agreeing) dramatically increases conviction. Conflict = lower confidence.

SYMBOL: {symbol}
CURRENT PRICE: {currentPrice:F2}

═══ COMBINED REGIME ═══
Regime: {combinedRegime} ({regimeConfidence:F0}% confidence)
Direction Alignment: {directionAlignment} (score: {alignmentScore:F0}%)

═══ TIMEFRAME BREAKDOWN ═══
{tfBreakdown}

═══ MARKET CONTEXT (Daily) ═══
{contextSection}

═══ KEY PRICE LEVELS ═══
{levelsSection}
{performanceSection}
═══ ANALYSIS RULES ═══
1. Weekly patterns OVERRIDE conflicting daily/4H patterns
2. All-timeframe alignment = high conviction (80%+)
3. Conflicting timeframes = low conviction, recommend Wait
4. Patterns at key support/resistance levels are MORE significant
5. Overbought RSI + bullish pattern = caution (potential exhaustion)
6. Breakout volume (>2x avg) confirms pattern validity
7. Price far from SMA200 (>15%) = extended, reversal risk higher{perfRules}

Respond in this EXACT JSON format (no markdown, no code blocks, just raw JSON):
{{
  ""overallBias"": ""<Strong Bullish/Bullish/Neutral/Bearish/Strong Bearish>"",
  ""overallConfidence"": <number 0-100>,
  ""summary"": ""<2-3 sentences: what the multi-timeframe picture tells us{perfSummaryHint}>"",
  ""detailedAnalysis"": ""<5-8 sentences: timeframe alignment analysis, key level proximity, volume confirmation, regime context, specific risks{perfDetailHint}>"",
  ""recommendedAction"": ""<Buy/Sell/Wait/Watch>"",
  ""recommendedEntry"": <price or null>,
  ""recommendedStop"": <price or null>,
  ""recommendedTarget"": <price or null>,
  ""riskReward"": ""<e.g. 1:2.5>"",
  ""patternVerdicts"": [
    {{
      ""patternName"": ""<n>"",
      ""isValid"": <true/false>,
      ""grade"": ""<A+/A/B+/B/C/D/F>"",
      ""oneLineReason"": ""<brief reason including which timeframe{perfVerdictHint}>""
    }}
  ]
}}";

        try
        {
            var response = await CallOllama(prompt);
            var analysis = ParseMultiPatternAnalysis(response);
            analysis.Symbol = symbol;
            return analysis;
        }
        catch
        {
            var bullish = topPatterns.Count(p => p.Direction == Domain.Enumerations.PatternDirection.Bullish);
            var bearish = topPatterns.Count(p => p.Direction == Domain.Enumerations.PatternDirection.Bearish);
            return new MultiPatternAnalysis
            {
                Symbol = symbol,
                OverallBias = bullish > bearish ? "Bullish" : bearish > bullish ? "Bearish" : "Neutral",
                OverallConfidence = topPatterns.Any() ? topPatterns.Average(p => p.Confidence) : 0,
                Summary = "AI synthesis unavailable — showing math-only analysis.",
                DetailedAnalysis = $"Multi-timeframe scan: {directionAlignment} direction alignment ({alignmentScore:F0}%). Combined regime: {combinedRegime}. Manual review recommended.",
                RecommendedAction = directionAlignment.Contains("All") ? "Watch" : "Wait",
                RiskReward = "N/A"
            };
        }
    }
}