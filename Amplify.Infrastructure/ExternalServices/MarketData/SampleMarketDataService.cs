using Amplify.Application.Common.Interfaces.Market;
using Amplify.Application.Common.Models;

namespace Amplify.Infrastructure.ExternalServices.MarketData;

/// <summary>
/// Generates random sample OHLCV data for testing when Alpaca is not configured.
/// This is the legacy behavior — produces deterministic pseudo-random candles.
/// </summary>
public class SampleMarketDataService : IMarketDataService
{
    public string DataSource => "Sample Data";

    public Task<bool> IsAvailableAsync() => Task.FromResult(true); // Always available

    public Task<List<Candle>> GetCandlesAsync(string symbol, int count, string timeframe)
    {
        var candles = GenerateSampleCandles(symbol, count, timeframe);
        return Task.FromResult(candles);
    }

    private static List<Candle> GenerateSampleCandles(string symbol, int count, string timeframe)
    {
        var candles = new List<Candle>();
        var random = new Random(symbol.GetHashCode() + timeframe.GetHashCode());

        var normalizedSymbol = symbol.ToUpper().Replace("/", "");
        var basePrice = normalizedSymbol switch
        {
            "TSLA" => 410m,
            "AAPL" => 265m,
            "MSFT" => 420m,
            "GOOGL" => 175m,
            "AMZN" => 200m,
            "NVDA" => 870m,
            "META" => 590m,
            "COIN" => 250m,
            "BTC" or "BTCUSD" => 98000m,
            "ETH" or "ETHUSD" => 2700m,
            "SOL" or "SOLUSD" => 170m,
            "DOGE" or "DOGEUSD" => 0.25m,
            "XRP" or "XRPUSD" => 2.50m,
            "ADA" or "ADAUSD" => 0.75m,
            _ => 150m
        };

        var volScale = timeframe switch
        {
            "1H" => 0.005m,
            "4H" => 0.008m,
            "Weekly" => 0.035m,
            _ => 0.02m
        };

        var price = basePrice * 0.85m;

        for (int i = count; i >= 0; i--)
        {
            DateTime date;
            if (timeframe == "1H")
            {
                date = DateTime.Today.AddHours(-i);
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
                // Only market hours: 9:30 AM - 4:00 PM ET (approx 14-20 UTC)
                if (date.Hour < 14 || date.Hour > 20) continue;
            }
            else if (timeframe == "4H")
            {
                date = DateTime.Today.AddHours(-i * 4);
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            }
            else if (timeframe == "Weekly")
            {
                date = DateTime.Today.AddDays(-i * 7);
            }
            else
            {
                date = DateTime.Today.AddDays(-i);
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday) continue;
            }

            var change = (decimal)(random.NextDouble() - 0.47) * basePrice * volScale;
            var open = price;
            var close = price + change;
            var high = Math.Max(open, close) + (decimal)random.NextDouble() * basePrice * volScale * 0.4m;
            var low = Math.Min(open, close) - (decimal)random.NextDouble() * basePrice * volScale * 0.4m;
            var volume = (5000000m + (decimal)random.Next(0, 15000000)) * (timeframe == "Weekly" ? 5 : timeframe == "4H" ? 0.25m : 1);

            candles.Add(new Candle
            {
                Time = date,
                Open = Math.Round(open, basePrice < 1m ? 6 : basePrice < 10m ? 4 : 2),
                High = Math.Round(high, basePrice < 1m ? 6 : basePrice < 10m ? 4 : 2),
                Low = Math.Round(low, basePrice < 1m ? 6 : basePrice < 10m ? 4 : 2),
                Close = Math.Round(close, basePrice < 1m ? 6 : basePrice < 10m ? 4 : 2),
                Volume = volume
            });
            price = close;
        }
        return candles;
    }
}