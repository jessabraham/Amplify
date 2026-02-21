namespace Amplify.Application.Common.Models;

/// <summary>
/// AI's analysis of a single detected pattern.
/// </summary>
public class PatternAnalysis
{
    public decimal AIConfidence { get; set; }       // 0-100 AI's confidence the pattern plays out
    public string Reasoning { get; set; } = "";      // Why the AI agrees/disagrees with the pattern
    public string RiskAssessment { get; set; } = ""; // What could go wrong
    public string TradePlan { get; set; } = "";      // Specific entry/exit strategy
    public bool IsValid { get; set; }                // AI thinks this is a real pattern (not noise)
    public string Grade { get; set; } = "";          // A+, A, B+, B, C, D, F
}

/// <summary>
/// AI's synthesis of multiple patterns on one symbol.
/// </summary>
public class MultiPatternAnalysis
{
    public string Symbol { get; set; } = "";
    public string OverallBias { get; set; } = "";         // "Strong Bullish", "Bullish", "Neutral", "Bearish", "Strong Bearish"
    public decimal OverallConfidence { get; set; }         // 0-100
    public string Summary { get; set; } = "";              // 2-3 sentence synthesis
    public string DetailedAnalysis { get; set; } = "";     // Full AI analysis
    public string RecommendedAction { get; set; } = "";    // "Buy", "Sell", "Wait", "Watch"
    public decimal? RecommendedEntry { get; set; }
    public decimal? RecommendedStop { get; set; }
    public decimal? RecommendedTarget { get; set; }
    public string RiskReward { get; set; } = "";           // e.g. "1:2.5"
    public List<PatternVerdict> PatternVerdicts { get; set; } = new();
}

/// <summary>
/// AI's verdict on each individual pattern within a multi-pattern analysis.
/// </summary>
public class PatternVerdict
{
    public string PatternName { get; set; } = "";
    public bool IsValid { get; set; }
    public string Grade { get; set; } = "";
    public string OneLineReason { get; set; } = "";
    public decimal? Entry { get; set; }
    public decimal? Stop { get; set; }
    public decimal? Target { get; set; }
}