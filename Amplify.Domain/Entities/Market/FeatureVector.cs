using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Market;

public class FeatureVector : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Symbol { get; set; } = string.Empty;

    // Technical indicators
    public decimal RSI { get; set; }
    public decimal MACD { get; set; }
    public decimal MACDSignal { get; set; }
    public decimal BollingerUpper { get; set; }
    public decimal BollingerLower { get; set; }
    public decimal ATR { get; set; }
    public decimal SMA20 { get; set; }
    public decimal SMA50 { get; set; }
    public decimal EMA12 { get; set; }
    public decimal EMA26 { get; set; }
    public decimal VWAP { get; set; }
    public long VolumeAvg20 { get; set; }

    public DateTime CalculatedAt { get; set; }
}