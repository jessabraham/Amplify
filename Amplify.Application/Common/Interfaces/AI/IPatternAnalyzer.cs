using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.AI;

/// <summary>
/// Uses AI (Ollama) to validate and enhance detected patterns.
/// </summary>
public interface IPatternAnalyzer
{
    /// <summary>
    /// Analyze a single detected pattern with AI.
    /// Returns enhanced analysis with AI confidence, reasoning, and trade plan.
    /// </summary>
    Task<PatternAnalysis> AnalyzePatternAsync(PatternResult pattern, List<Candle> recentCandles, string symbol);

    /// <summary>
    /// Analyze multiple patterns together and synthesize a unified recommendation.
    /// </summary>
    Task<MultiPatternAnalysis> SynthesizePatternsAsync(List<PatternResult> patterns, List<Candle> recentCandles, string symbol);
}