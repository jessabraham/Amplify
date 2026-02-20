using Amplify.Application.Common.DTOs.Market;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Domain.Enumerations;

namespace Amplify.Infrastructure.ExternalServices.Trading;

/// <summary>
/// Rule-based market regime classifier.
/// 
/// Classification logic:
///   1. Trending:       Strong SMA slope + price above/below SMA50 + ADX-like conditions
///   2. VolExpansion:   High ATR% + wide Bollinger Bands + extreme RSI
///   3. MeanReversion:  RSI extreme + narrow bands + price near Bollinger edge
///   4. Choppy:         Flat SMA slope + mid-range RSI + narrow ATR — the default
///
/// Each regime produces a confidence score (0-100) based on how many
/// confirming signals are present. The highest-confidence regime wins.
/// </summary>
public class RegimeEngine : IRegimeEngine
{
    public RegimeResultDto DetectRegime(FeatureVectorDto f)
    {
        var candidates = new List<(MarketRegime regime, decimal confidence, string rationale)>
        {
            ScoreTrending(f),
            ScoreVolExpansion(f),
            ScoreMeanReversion(f),
            ScoreChoppy(f)
        };

        var winner = candidates.OrderByDescending(c => c.confidence).First();

        return new RegimeResultDto
        {
            Symbol = f.Symbol,
            Regime = winner.regime,
            Confidence = Math.Round(winner.confidence, 1),
            Rationale = winner.rationale,
            Features = f,
            DetectedAt = DateTime.UtcNow
        };
    }

    // ── Trending ─────────────────────────────────────────────────────

    private static (MarketRegime, decimal, string) ScoreTrending(FeatureVectorDto f)
    {
        var score = 0m;
        var reasons = new List<string>();

        // Strong SMA20 slope (> 0.5% over 5 bars)
        var absSlope = Math.Abs(f.SMA20Slope);
        if (absSlope > 1.0m) { score += 30; reasons.Add($"Strong SMA20 slope ({f.SMA20Slope:F2}%)"); }
        else if (absSlope > 0.5m) { score += 20; reasons.Add($"Moderate SMA20 slope ({f.SMA20Slope:F2}%)"); }

        // Price trending relative to SMA50
        if (f.CurrentPrice > f.SMA50 && f.SMA20 > f.SMA50)
        { score += 20; reasons.Add("Price & SMA20 above SMA50 (uptrend)"); }
        else if (f.CurrentPrice < f.SMA50 && f.SMA20 < f.SMA50)
        { score += 20; reasons.Add("Price & SMA20 below SMA50 (downtrend)"); }

        // MACD confirming direction
        if ((f.SMA20Slope > 0 && f.MACD > f.MACDSignal) ||
            (f.SMA20Slope < 0 && f.MACD < f.MACDSignal))
        { score += 15; reasons.Add("MACD confirms trend direction"); }

        // RSI trending (not at extremes, but showing directional bias)
        if (f.RSI > 55 && f.RSI < 75 && f.SMA20Slope > 0)
        { score += 10; reasons.Add($"RSI bullish range ({f.RSI:F1})"); }
        else if (f.RSI > 25 && f.RSI < 45 && f.SMA20Slope < 0)
        { score += 10; reasons.Add($"RSI bearish range ({f.RSI:F1})"); }

        // Moderate ATR (not too wide = explosion, not too narrow = dead)
        if (f.ATRPercent > 1.0m && f.ATRPercent < 3.5m)
        { score += 10; reasons.Add($"Healthy ATR% ({f.ATRPercent:F2}%)"); }

        // EMA alignment
        if ((f.EMA12 > f.EMA26 && f.SMA20Slope > 0) ||
            (f.EMA12 < f.EMA26 && f.SMA20Slope < 0))
        { score += 10; reasons.Add("EMA12/26 aligned with trend"); }

        return (MarketRegime.Trending, Math.Min(score, 100), string.Join("; ", reasons));
    }

    // ── Volatility Expansion ─────────────────────────────────────────

    private static (MarketRegime, decimal, string) ScoreVolExpansion(FeatureVectorDto f)
    {
        var score = 0m;
        var reasons = new List<string>();

        // High ATR%
        if (f.ATRPercent > 4.0m) { score += 30; reasons.Add($"Very high ATR% ({f.ATRPercent:F2}%)"); }
        else if (f.ATRPercent > 3.0m) { score += 20; reasons.Add($"High ATR% ({f.ATRPercent:F2}%)"); }

        // Wide Bollinger Bands
        if (f.BollingerWidth > 8.0m) { score += 25; reasons.Add($"Wide Bollinger Bands ({f.BollingerWidth:F2}%)"); }
        else if (f.BollingerWidth > 5.0m) { score += 15; reasons.Add($"Expanding Bollinger Bands ({f.BollingerWidth:F2}%)"); }

        // Extreme RSI
        if (f.RSI > 75 || f.RSI < 25)
        { score += 20; reasons.Add($"Extreme RSI ({f.RSI:F1})"); }

        // MACD diverging from signal
        var macdGap = Math.Abs(f.MACD - f.MACDSignal);
        if (f.CurrentPrice != 0 && macdGap / f.CurrentPrice * 100m > 0.5m)
        { score += 15; reasons.Add("Large MACD/Signal divergence"); }

        // Price outside Bollinger Bands
        if (f.CurrentPrice > f.BollingerUpper || f.CurrentPrice < f.BollingerLower)
        { score += 15; reasons.Add("Price outside Bollinger Bands"); }

        return (MarketRegime.VolExpansion, Math.Min(score, 100), string.Join("; ", reasons));
    }

    // ── Mean Reversion ───────────────────────────────────────────────

    private static (MarketRegime, decimal, string) ScoreMeanReversion(FeatureVectorDto f)
    {
        var score = 0m;
        var reasons = new List<string>();

        // RSI at extremes suggesting reversion
        if (f.RSI > 70) { score += 25; reasons.Add($"Overbought RSI ({f.RSI:F1}) — reversion likely"); }
        else if (f.RSI < 30) { score += 25; reasons.Add($"Oversold RSI ({f.RSI:F1}) — reversion likely"); }

        // Price near Bollinger Band edge but bands not super wide
        if (f.BollingerWidth < 6.0m)
        {
            if (f.CurrentPrice >= f.BollingerUpper * 0.98m)
            { score += 20; reasons.Add("Price at upper Bollinger Band"); }
            else if (f.CurrentPrice <= f.BollingerLower * 1.02m)
            { score += 20; reasons.Add("Price at lower Bollinger Band"); }
        }

        // Flat SMA slope (range-bound)
        if (Math.Abs(f.SMA20Slope) < 0.3m)
        { score += 15; reasons.Add($"Flat SMA20 slope ({f.SMA20Slope:F2}%) — range-bound"); }

        // MACD near zero / signal crossing
        if (Math.Abs(f.MACD) < Math.Abs(f.MACDSignal) * 0.3m || Math.Abs(f.MACD - f.MACDSignal) < 0.01m)
        { score += 10; reasons.Add("MACD converging — momentum fading"); }

        // Moderate ATR (not extreme)
        if (f.ATRPercent > 1.0m && f.ATRPercent < 3.0m)
        { score += 10; reasons.Add($"Moderate volatility ({f.ATRPercent:F2}%)"); }

        // Price near VWAP (gravitating to mean)
        if (f.VWAP > 0 && f.CurrentPrice > 0)
        {
            var vwapDist = Math.Abs(f.CurrentPrice - f.VWAP) / f.CurrentPrice * 100m;
            if (vwapDist < 1.0m)
            { score += 10; reasons.Add("Price near VWAP"); }
        }

        return (MarketRegime.MeanReversion, Math.Min(score, 100), string.Join("; ", reasons));
    }

    // ── Choppy ───────────────────────────────────────────────────────

    private static (MarketRegime, decimal, string) ScoreChoppy(FeatureVectorDto f)
    {
        var score = 0m;
        var reasons = new List<string>();

        // Flat SMA slope
        if (Math.Abs(f.SMA20Slope) < 0.3m)
        { score += 25; reasons.Add($"Flat SMA20 slope ({f.SMA20Slope:F2}%)"); }
        else if (Math.Abs(f.SMA20Slope) < 0.5m)
        { score += 15; reasons.Add($"Nearly flat SMA20 slope ({f.SMA20Slope:F2}%)"); }

        // RSI mid-range (no conviction)
        if (f.RSI > 40 && f.RSI < 60)
        { score += 20; reasons.Add($"Mid-range RSI ({f.RSI:F1}) — no directional conviction"); }

        // Narrow Bollinger Bands
        if (f.BollingerWidth < 3.0m)
        { score += 20; reasons.Add($"Narrow Bollinger Bands ({f.BollingerWidth:F2}%)"); }
        else if (f.BollingerWidth < 4.5m)
        { score += 10; reasons.Add($"Tight Bollinger Bands ({f.BollingerWidth:F2}%)"); }

        // Low ATR
        if (f.ATRPercent < 1.5m)
        { score += 15; reasons.Add($"Low ATR% ({f.ATRPercent:F2}%)"); }

        // MACD near zero line
        if (f.CurrentPrice != 0 && Math.Abs(f.MACD) / f.CurrentPrice * 100m < 0.2m)
        { score += 10; reasons.Add("MACD near zero — no momentum"); }

        // SMAs intertwined
        if (f.SMA20 > 0 && Math.Abs(f.SMA20 - f.SMA50) / f.SMA20 * 100m < 1.0m)
        { score += 10; reasons.Add("SMA20 and SMA50 converged"); }

        return (MarketRegime.Choppy, Math.Min(score, 100), string.Join("; ", reasons));
    }
}