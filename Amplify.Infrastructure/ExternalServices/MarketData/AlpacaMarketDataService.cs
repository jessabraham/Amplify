using Alpaca.Markets;
using Amplify.Application.Common.Interfaces.Market;
using Amplify.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AlpacaEnvironments = Alpaca.Markets.Environments;
using Candle = Amplify.Application.Common.Models.Candle;

namespace Amplify.Infrastructure.ExternalServices.MarketData;

/// <summary>
/// Fetches real OHLCV data from Alpaca Markets API.
/// Supports both stocks (IEX feed) and crypto (BTC/USD, ETH/USD, etc.).
/// Free tier provides IEX stock data and full crypto data.
/// </summary>
public class AlpacaMarketDataService : IMarketDataService
{
    private readonly AlpacaSettings _settings;
    private readonly ILogger<AlpacaMarketDataService> _logger;
    private IAlpacaDataClient? _stockClient;
    private IAlpacaCryptoDataClient? _cryptoClient;
    private bool? _isAvailable;

    public string DataSource => "Alpaca";

    // Known crypto symbols — anything with USD/BTC/ETH suffix or containing a slash
    private static readonly HashSet<string> CryptoSymbols = new(StringComparer.OrdinalIgnoreCase)
    {
        "BTCUSD", "BTC/USD", "ETHUSD", "ETH/USD", "SOLUSD", "SOL/USD",
        "DOGEUSD", "DOGE/USD", "AVAXUSD", "AVAX/USD", "LINKUSD", "LINK/USD",
        "DOTUSD", "DOT/USD", "MATICUSD", "MATIC/USD", "UNIUSD", "UNI/USD",
        "AAVEUSD", "AAVE/USD", "LTCUSD", "LTC/USD", "BCHUSD", "BCH/USD",
        "SHIBUSD", "SHIB/USD", "XRPUSD", "XRP/USD", "ADAUSD", "ADA/USD",
        "ATOMUSD", "ATOM/USD", "NEARUSD", "NEAR/USD", "ARBUSD", "ARB/USD",
        "OPUSD", "OP/USD", "APTUSD", "APT/USD", "SUIUSD", "SUI/USD",
        "PEPEUSD", "PEPE/USD", "WBTCUSD", "WBTC/USD", "MKRUSD", "MKR/USD"
    };

    public AlpacaMarketDataService(IOptions<AlpacaSettings> settings, ILogger<AlpacaMarketDataService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Detect if a symbol is a crypto trading pair.
    /// Matches known symbols or any symbol ending in USD that's not a real stock ticker.
    /// </summary>
    private static bool IsCryptoSymbol(string symbol)
    {
        if (string.IsNullOrEmpty(symbol)) return false;
        // If it contains a slash, it's crypto (BTC/USD)
        if (symbol.Contains('/')) return true;
        // Check known crypto symbols
        return CryptoSymbols.Contains(symbol);
    }

    /// <summary>
    /// Normalize a crypto symbol to Alpaca format: BTC/USD
    /// Handles inputs like BTCUSD, btcusd, BTC/USD
    /// </summary>
    private static string NormalizeCryptoSymbol(string symbol)
    {
        var upper = symbol.ToUpper().Trim();
        // Already has slash
        if (upper.Contains('/')) return upper;
        // Strip "USD" suffix and add slash
        if (upper.EndsWith("USD") && upper.Length > 3)
            return upper[..^3] + "/USD";
        return upper;
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (_isAvailable.HasValue) return _isAvailable.Value;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            _logger.LogWarning("Alpaca API keys not configured — using sample data fallback");
            _isAvailable = false;
            return false;
        }

        try
        {
            var client = GetStockClient();
            // Quick health check — fetch 1 bar for AAPL
            var request = new HistoricalBarsRequest("AAPL", DateTime.UtcNow.AddDays(-5), DateTime.UtcNow, BarTimeFrame.Day);
            request.Feed = MarketDataFeed.Iex;  // Free tier requires IEX feed
            request.Pagination.Size = 1;
            var response = await client.ListHistoricalBarsAsync(request);
            _isAvailable = response.Items.Any();
            _logger.LogInformation("Alpaca connection {Status}", _isAvailable.Value ? "OK" : "FAILED (no data)");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Alpaca connection failed — will use sample data fallback");
            _isAvailable = false;
        }

        return _isAvailable.Value;
    }

    public async Task<List<Candle>> GetCandlesAsync(string symbol, int count, string timeframe)
    {
        if (IsCryptoSymbol(symbol))
            return await GetCryptoCandlesAsync(symbol, count, timeframe);
        else
            return await GetStockCandlesAsync(symbol, count, timeframe);
    }

    // ═══════════════════════════════════════════════════════════════
    // STOCK DATA (IEX feed)
    // ═══════════════════════════════════════════════════════════════

    private async Task<List<Candle>> GetStockCandlesAsync(string symbol, int count, string timeframe)
    {
        var client = GetStockClient();
        var barTimeFrame = MapTimeframe(timeframe);
        var (from, to) = CalculateDateRange(count, timeframe, isCrypto: false);

        _logger.LogDebug("Fetching {Count} {Timeframe} stock bars for {Symbol}", count, timeframe, symbol);

        try
        {
            var request = new HistoricalBarsRequest(symbol.ToUpper(), from, to, barTimeFrame);
            request.Feed = MarketDataFeed.Iex;  // Free tier requires IEX feed
            request.Pagination.Size = (uint)Math.Min(count + 20, 10000);

            var allBars = new List<IBar>();
            do
            {
                var response = await client.ListHistoricalBarsAsync(request);
                allBars.AddRange(response.Items);
                if (allBars.Count >= count + 20) break;
            } while (false); // Simplified — single page is usually enough

            return ConvertBars(allBars, count, symbol, timeframe);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Timeframe} bars for {Symbol} from Alpaca", timeframe, symbol);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // CRYPTO DATA (no feed restriction, 24/7)
    // ═══════════════════════════════════════════════════════════════

    private async Task<List<Candle>> GetCryptoCandlesAsync(string symbol, int count, string timeframe)
    {
        var client = GetCryptoClient();
        var cryptoSymbol = NormalizeCryptoSymbol(symbol);
        var barTimeFrame = MapTimeframe(timeframe);
        var (from, to) = CalculateDateRange(count, timeframe, isCrypto: true);

        _logger.LogDebug("Fetching {Count} {Timeframe} crypto bars for {Symbol} (normalized: {CryptoSymbol})",
            count, timeframe, symbol, cryptoSymbol);

        try
        {
            var request = new HistoricalCryptoBarsRequest(cryptoSymbol, from, to, barTimeFrame);
            request.Pagination.Size = (uint)Math.Min(count + 20, 10000);

            var allBars = new List<IBar>();
            do
            {
                var response = await client.ListHistoricalBarsAsync(request);
                allBars.AddRange(response.Items);
                if (allBars.Count >= count + 20) break;
            } while (false);

            var candles = ConvertBars(allBars, count, cryptoSymbol, timeframe);

            _logger.LogInformation("Fetched {Count} {Timeframe} crypto bars for {Symbol} from Alpaca (price: ${Price:F2})",
                candles.Count, timeframe, cryptoSymbol, candles.LastOrDefault()?.Close ?? 0);

            return candles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch {Timeframe} crypto bars for {Symbol} from Alpaca", timeframe, cryptoSymbol);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    private List<Candle> ConvertBars(List<IBar> bars, int count, string symbol, string timeframe)
    {
        var candles = bars
            .OrderBy(b => b.TimeUtc)
            .TakeLast(count)
            .Select(b => new Candle
            {
                Time = b.TimeUtc,
                Open = b.Open,
                High = b.High,
                Low = b.Low,
                Close = b.Close,
                Volume = b.Volume
            })
            .ToList();

        _logger.LogInformation("Fetched {Count} {Timeframe} bars for {Symbol} from Alpaca",
            candles.Count, timeframe, symbol);

        return candles;
    }

    private IAlpacaDataClient GetStockClient()
    {
        if (_stockClient is not null) return _stockClient;

        var key = new SecretKey(_settings.ApiKey!, _settings.ApiSecret!);
        _stockClient = _settings.UsePaper
            ? AlpacaEnvironments.Paper.GetAlpacaDataClient(key)
            : AlpacaEnvironments.Live.GetAlpacaDataClient(key);

        return _stockClient;
    }

    private IAlpacaCryptoDataClient GetCryptoClient()
    {
        if (_cryptoClient is not null) return _cryptoClient;

        var key = new SecretKey(_settings.ApiKey!, _settings.ApiSecret!);
        _cryptoClient = _settings.UsePaper
            ? AlpacaEnvironments.Paper.GetAlpacaCryptoDataClient(key)
            : AlpacaEnvironments.Live.GetAlpacaCryptoDataClient(key);

        return _cryptoClient;
    }

    private static BarTimeFrame MapTimeframe(string timeframe) => timeframe.ToUpper() switch
    {
        "1H" => BarTimeFrame.Hour,
        "4H" => new BarTimeFrame(4, BarTimeFrameUnit.Hour),
        "DAILY" or "1D" => BarTimeFrame.Day,
        "WEEKLY" or "1W" => BarTimeFrame.Week,
        "MONTHLY" or "1M" => BarTimeFrame.Month,
        _ => BarTimeFrame.Day
    };

    /// <summary>
    /// Calculate the from/to date range needed to get approximately 'count' bars.
    /// Crypto trades 24/7 so needs less calendar buffer than stocks.
    /// </summary>
    private static (DateTime from, DateTime to) CalculateDateRange(int count, string timeframe, bool isCrypto)
    {
        var to = DateTime.UtcNow;

        int calendarDays;
        if (isCrypto)
        {
            // Crypto trades 24/7 — 1 bar per day, every day
            calendarDays = timeframe.ToUpper() switch
            {
                "1H" => (int)(count / 24.0) + 3,           // 24 bars per day
                "4H" => (int)(count * 4.0 / 24.0) + 3,    // 6 bars per day
                "DAILY" or "1D" => count + 5,               // 1:1 calendar days
                "WEEKLY" or "1W" => count * 7 + 7,
                "MONTHLY" or "1M" => count * 31 + 31,
                _ => count + 5
            };
        }
        else
        {
            // Stocks trade ~252 days/year, need buffer for weekends/holidays
            calendarDays = timeframe.ToUpper() switch
            {
                "1H" => (int)(count / 6.5 * 1.5) + 5,     // ~6.5 trading hours/day
                "4H" => (int)(count * 4.0 / 6.5 * 1.5) + 5,
                "DAILY" or "1D" => (int)(count * 1.5) + 5,
                "WEEKLY" or "1W" => count * 7 + 14,
                "MONTHLY" or "1M" => count * 31 + 31,
                _ => (int)(count * 1.5) + 5
            };
        }

        var from = to.AddDays(-calendarDays);
        return (from, to);
    }
}

/// <summary>
/// Configuration for Alpaca API connection.
/// </summary>
public class AlpacaSettings
{
    public const string SectionName = "Alpaca";

    public string? ApiKey { get; set; }
    public string? ApiSecret { get; set; }
    public bool UsePaper { get; set; } = true; // Default to paper/free tier
}