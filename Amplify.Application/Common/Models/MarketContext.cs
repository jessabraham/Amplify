namespace Amplify.Application.Common.Models;

/// <summary>
/// Context layers computed from candle data to give the AI richer analysis.
/// </summary>
public class MarketContext
{
    // Volume analysis
    public decimal CurrentVolume { get; set; }
    public decimal AvgVolume20 { get; set; }
    public decimal VolumeRatio { get; set; }         // current / avg (>2 = breakout volume)
    public string VolumeProfile { get; set; } = "";   // "High", "Normal", "Low", "Breakout"

    // Key price levels
    public decimal CurrentPrice { get; set; }
    public List<KeyPriceLevel> KeyLevels { get; set; } = new();
    public decimal? NearestSupport { get; set; }
    public decimal? NearestResistance { get; set; }
    public decimal? DistanceToSupportPct { get; set; }
    public decimal? DistanceToResistancePct { get; set; }

    // Moving average context
    public decimal? SMA20 { get; set; }
    public decimal? SMA50 { get; set; }
    public decimal? SMA200 { get; set; }
    public decimal? DistFromSMA20Pct { get; set; }   // + = above, - = below
    public decimal? DistFromSMA50Pct { get; set; }
    public decimal? DistFromSMA200Pct { get; set; }
    public string MAAlignment { get; set; } = "";     // "Bullish Stack", "Bearish Stack", "Mixed"

    // Momentum
    public decimal? RSI { get; set; }
    public string RSIZone { get; set; } = "";         // "Oversold", "Neutral", "Overbought"
    public decimal? ATR { get; set; }
    public decimal? ATRPercent { get; set; }          // ATR as % of price

    // Trend strength
    public int ConsecutiveUpDays { get; set; }
    public int ConsecutiveDownDays { get; set; }
    public decimal? TrendSlope20 { get; set; }        // slope of SMA20 (positive = up)

    // Timeframe-specific regime
    public string Timeframe { get; set; } = "";
    public string Regime { get; set; } = "";
    public decimal RegimeConfidence { get; set; }
}

public class KeyPriceLevel
{
    public decimal Price { get; set; }
    public string Type { get; set; } = "";    // "Support", "Resistance", "Round Number", "Prior Swing High/Low"
    public int TouchCount { get; set; }        // how many times price bounced here
    public string Description { get; set; } = "";
}

/// <summary>
/// Combined multi-timeframe scan result with all context layers.
/// </summary>
public class MultiTimeframeScanResult
{
    public string Symbol { get; set; } = "";
    public decimal CurrentPrice { get; set; }

    // Per-timeframe data
    public List<TimeframeData> Timeframes { get; set; } = new();

    // Combined regime (weighted)
    public string CombinedRegime { get; set; } = "";
    public decimal CombinedRegimeConfidence { get; set; }
    public string RegimeAlignment { get; set; } = "";  // "Aligned", "Mixed", "Conflicting"

    // Timeframe agreement
    public string DirectionAlignment { get; set; } = "";  // "All Bullish", "All Bearish", "Mixed", "Conflicting"
    public decimal AlignmentScore { get; set; }            // 0-100, higher = more agreement

    // Deduplicated best patterns (one per type, strongest timeframe)
    public List<PatternResult> TopPatterns { get; set; } = new();

    // Market context (from daily)
    public MarketContext DailyContext { get; set; } = new();
}

public class TimeframeData
{
    public string Timeframe { get; set; } = "";       // "4H", "Daily", "Weekly"
    public decimal Weight { get; set; }                // 1.0, 2.0, 3.0
    public List<PatternResult> Patterns { get; set; } = new();
    public MarketContext Context { get; set; } = new();
    public string DominantDirection { get; set; } = ""; // "Bullish", "Bearish", "Neutral"
    public int BullishCount { get; set; }
    public int BearishCount { get; set; }
    public int NeutralCount { get; set; }
}