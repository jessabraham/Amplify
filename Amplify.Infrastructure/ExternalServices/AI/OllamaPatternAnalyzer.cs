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
}