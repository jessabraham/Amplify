using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.AI;

/// <summary>
/// Uses AI (Ollama) to validate and enhance detected patterns.
/// </summary>
public interface IPatternAnalyzer
{
    /// <summary>
    /// Analyze a single detected pattern with AI.
    /// </summary>
    Task<PatternAnalysis> AnalyzePatternAsync(PatternResult pattern, List<Candle> recentCandles, string symbol);

    /// <summary>
    /// Analyze multiple patterns together and synthesize a unified recommendation.
    /// </summary>
    Task<MultiPatternAnalysis> SynthesizePatternsAsync(List<PatternResult> patterns, List<Candle> recentCandles, string symbol);

    /// <summary>
    /// Multi-timeframe synthesis: patterns from all timeframes + context layers + combined regime + historical performance.
    /// </summary>
    Task<MultiPatternAnalysis> SynthesizeMultiTimeframeAsync(
        List<PatternResult> topPatterns,
        List<TimeframeData> timeframes,
        MarketContext dailyContext,
        string combinedRegime,
        decimal regimeConfidence,
        string directionAlignment,
        decimal alignmentScore,
        string symbol,
        List<PatternPerformanceData>? performanceStats = null,
        UserStatsData? userStats = null);
}

/// <summary>
/// Pattern performance data passed to the AI for feedback-driven recommendations.
/// </summary>
public class PatternPerformanceData
{
    public string PatternName { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public string Regime { get; set; } = "";
    public int TotalTrades { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public decimal WinRate { get; set; }
    public decimal AvgRMultiple { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal WinRateWhenAligned { get; set; }
    public int TradesWhenAligned { get; set; }
    public decimal WinRateWhenConflicting { get; set; }
    public int TradesWhenConflicting { get; set; }
    public decimal WinRateWithBreakoutVol { get; set; }
    public int TradesWithBreakoutVol { get; set; }
    public decimal AvgDaysHeld { get; set; }
}

/// <summary>
/// User's overall stats passed to AI for personalized advice.
/// </summary>
public class UserStatsData
{
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal AvgRMultiple { get; set; }
    public decimal LongWinRate { get; set; }
    public decimal ShortWinRate { get; set; }
    public decimal AlignedWinRate { get; set; }
    public decimal ConflictingWinRate { get; set; }
}