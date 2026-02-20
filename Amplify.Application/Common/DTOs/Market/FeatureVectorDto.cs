namespace Amplify.Application.Common.DTOs.Market;

public class FeatureVectorDto
{
    public string Symbol { get; set; } = string.Empty;
    public decimal RSI { get; set; }
    public decimal MACD { get; set; }
    public decimal MACDSignal { get; set; }
    public decimal BollingerUpper { get; set; }
    public decimal BollingerLower { get; set; }
    public decimal BollingerWidth { get; set; }
    public decimal ATR { get; set; }
    public decimal ATRPercent { get; set; }
    public decimal SMA20 { get; set; }
    public decimal SMA50 { get; set; }
    public decimal EMA12 { get; set; }
    public decimal EMA26 { get; set; }
    public decimal VWAP { get; set; }
    public long VolumeAvg20 { get; set; }
    public decimal SMA20Slope { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime CalculatedAt { get; set; }
}