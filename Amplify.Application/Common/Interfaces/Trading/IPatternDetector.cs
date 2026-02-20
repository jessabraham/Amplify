using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.Trading;

/// <summary>
/// Scans price data and detects candlestick patterns, chart patterns, and technical setups.
/// </summary>
public interface IPatternDetector
{
    /// <summary>
    /// Scan candles for all detectable patterns.
    /// </summary>
    List<PatternResult> DetectAll(List<Candle> candles);

    /// <summary>
    /// Scan for candlestick patterns only (single, double, triple).
    /// </summary>
    List<PatternResult> DetectCandlestickPatterns(List<Candle> candles);

    /// <summary>
    /// Scan for chart patterns only (H&S, double top, triangles, etc).
    /// </summary>
    List<PatternResult> DetectChartPatterns(List<Candle> candles);

    /// <summary>
    /// Scan for technical setups only (golden cross, RSI divergence, etc).
    /// </summary>
    List<PatternResult> DetectTechnicalSetups(List<Candle> candles);
}