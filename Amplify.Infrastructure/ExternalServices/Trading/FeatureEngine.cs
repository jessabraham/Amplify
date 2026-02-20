using Amplify.Application.Common.DTOs.Market;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;

namespace Amplify.Infrastructure.ExternalServices.Trading;

public class FeatureEngine : IFeatureEngine
{
    public FeatureVectorDto ComputeFeatures(string symbol, List<Candle> candles)
    {
        if (candles.Count < 50)
            throw new ArgumentException("Need at least 50 candles to compute features.");

        var closes = candles.Select(c => c.Close).ToList();
        var highs = candles.Select(c => c.High).ToList();
        var lows = candles.Select(c => c.Low).ToList();
        var volumes = candles.Select(c => c.Volume).ToList();
        var currentPrice = closes[^1];

        var rsi = ComputeRSI(closes, 14);
        var (macd, signal) = ComputeMACD(closes);
        var sma20 = SMA(closes, 20);
        var sma50 = SMA(closes, 50);
        var ema12 = EMA(closes, 12);
        var ema26 = EMA(closes, 26);
        var (bbUpper, bbLower) = ComputeBollingerBands(closes, 20, 2m);
        var atr = ComputeATR(highs, lows, closes, 14);
        var vwap = ComputeVWAP(candles);
        var volAvg20 = (long)volumes.TakeLast(20).Average();

        // SMA20 slope: change over last 5 periods as percentage
        var sma20Values = ComputeSMASeries(closes, 20);
        var sma20Slope = 0m;
        if (sma20Values.Count >= 5 && sma20Values[^5] != 0)
            sma20Slope = (sma20Values[^1] - sma20Values[^5]) / sma20Values[^5] * 100m;

        var bbWidth = sma20 != 0 ? (bbUpper - bbLower) / sma20 * 100m : 0m;
        var atrPercent = currentPrice != 0 ? atr / currentPrice * 100m : 0m;

        return new FeatureVectorDto
        {
            Symbol = symbol,
            RSI = Math.Round(rsi, 4),
            MACD = Math.Round(macd, 4),
            MACDSignal = Math.Round(signal, 4),
            BollingerUpper = Math.Round(bbUpper, 4),
            BollingerLower = Math.Round(bbLower, 4),
            BollingerWidth = Math.Round(bbWidth, 4),
            ATR = Math.Round(atr, 4),
            ATRPercent = Math.Round(atrPercent, 4),
            SMA20 = Math.Round(sma20, 4),
            SMA50 = Math.Round(sma50, 4),
            EMA12 = Math.Round(ema12, 4),
            EMA26 = Math.Round(ema26, 4),
            VWAP = Math.Round(vwap, 4),
            VolumeAvg20 = volAvg20,
            SMA20Slope = Math.Round(sma20Slope, 4),
            CurrentPrice = currentPrice,
            CalculatedAt = DateTime.UtcNow
        };
    }

    // ── RSI ──────────────────────────────────────────────────────────

    private static decimal ComputeRSI(List<decimal> closes, int period)
    {
        if (closes.Count < period + 1) return 50m;

        var gains = new List<decimal>();
        var losses = new List<decimal>();

        for (int i = 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            gains.Add(change > 0 ? change : 0);
            losses.Add(change < 0 ? Math.Abs(change) : 0);
        }

        var avgGain = gains.Take(period).Average();
        var avgLoss = losses.Take(period).Average();

        for (int i = period; i < gains.Count; i++)
        {
            avgGain = (avgGain * (period - 1) + gains[i]) / period;
            avgLoss = (avgLoss * (period - 1) + losses[i]) / period;
        }

        if (avgLoss == 0) return 100m;
        var rs = avgGain / avgLoss;
        return 100m - (100m / (1m + rs));
    }

    // ── MACD ─────────────────────────────────────────────────────────

    private static (decimal macd, decimal signal) ComputeMACD(List<decimal> closes)
    {
        var ema12 = EMA(closes, 12);
        var ema26 = EMA(closes, 26);
        var macd = ema12 - ema26;

        // Approximate signal line from MACD series
        var macdSeries = new List<decimal>();
        var ema12Series = ComputeEMASeries(closes, 12);
        var ema26Series = ComputeEMASeries(closes, 26);
        var minLen = Math.Min(ema12Series.Count, ema26Series.Count);
        for (int i = 0; i < minLen; i++)
            macdSeries.Add(ema12Series[ema12Series.Count - minLen + i] - ema26Series[ema26Series.Count - minLen + i]);

        var signal = macdSeries.Count >= 9 ? EMA(macdSeries, 9) : macd;
        return (macd, signal);
    }

    // ── Bollinger Bands ──────────────────────────────────────────────

    private static (decimal upper, decimal lower) ComputeBollingerBands(
        List<decimal> closes, int period, decimal multiplier)
    {
        var sma = SMA(closes, period);
        var recentCloses = closes.TakeLast(period).ToList();
        var variance = recentCloses.Average(c => (double)(c - sma) * (double)(c - sma));
        var stdDev = (decimal)Math.Sqrt(variance);
        return (sma + multiplier * stdDev, sma - multiplier * stdDev);
    }

    // ── ATR ──────────────────────────────────────────────────────────

    private static decimal ComputeATR(List<decimal> highs, List<decimal> lows,
        List<decimal> closes, int period)
    {
        var trueRanges = new List<decimal>();
        for (int i = 1; i < highs.Count; i++)
        {
            var tr = Math.Max(
                highs[i] - lows[i],
                Math.Max(
                    Math.Abs(highs[i] - closes[i - 1]),
                    Math.Abs(lows[i] - closes[i - 1])));
            trueRanges.Add(tr);
        }

        if (trueRanges.Count < period) return trueRanges.Any() ? trueRanges.Average() : 0m;

        var atr = trueRanges.Take(period).Average();
        for (int i = period; i < trueRanges.Count; i++)
            atr = (atr * (period - 1) + trueRanges[i]) / period;

        return atr;
    }

    // ── VWAP ─────────────────────────────────────────────────────────

    private static decimal ComputeVWAP(List<Candle> candles)
    {
        var totalVolPrice = 0m;
        var totalVol = 0m;
        foreach (var c in candles)
        {
            var typical = (c.High + c.Low + c.Close) / 3m;
            totalVolPrice += typical * c.Volume;
            totalVol += c.Volume;
        }
        return totalVol > 0 ? totalVolPrice / totalVol : 0m;
    }

    // ── Moving Averages ──────────────────────────────────────────────

    private static decimal SMA(List<decimal> values, int period)
    {
        if (values.Count < period) return values.Any() ? values.Average() : 0m;
        return values.TakeLast(period).Average();
    }

    private static List<decimal> ComputeSMASeries(List<decimal> values, int period)
    {
        var result = new List<decimal>();
        for (int i = period - 1; i < values.Count; i++)
            result.Add(values.Skip(i - period + 1).Take(period).Average());
        return result;
    }

    private static decimal EMA(List<decimal> values, int period)
    {
        var series = ComputeEMASeries(values, period);
        return series.Any() ? series[^1] : 0m;
    }

    private static List<decimal> ComputeEMASeries(List<decimal> values, int period)
    {
        if (values.Count < period) return values.ToList();
        var multiplier = 2m / (period + 1);
        var result = new List<decimal> { values.Take(period).Average() };
        for (int i = period; i < values.Count; i++)
            result.Add((values[i] - result[^1]) * multiplier + result[^1]);
        return result;
    }
}