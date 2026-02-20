namespace Amplify.Application.Common.Models;

/// <summary>
/// Represents a single OHLCV candlestick bar.
/// Used by the pattern detection engine.
/// </summary>
public class Candle
{
    public DateTime Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }

    // Computed properties
    public decimal Body => Math.Abs(Close - Open);
    public decimal UpperWick => High - Math.Max(Open, Close);
    public decimal LowerWick => Math.Min(Open, Close) - Low;
    public decimal Range => High - Low;
    public bool IsBullish => Close > Open;
    public bool IsBearish => Close < Open;
    public bool IsDoji => Range > 0 && Body / Range < 0.1m;
    public decimal MidPoint => (High + Low) / 2m;
    public decimal BodyCenter => (Open + Close) / 2m;
}