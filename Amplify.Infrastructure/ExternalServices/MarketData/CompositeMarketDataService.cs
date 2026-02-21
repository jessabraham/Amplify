using Amplify.Application.Common.Interfaces.Market;
using Amplify.Application.Common.Models;
using Microsoft.Extensions.Logging;

namespace Amplify.Infrastructure.ExternalServices.MarketData;

/// <summary>
/// Composite market data service: tries Alpaca first, falls back to sample data.
/// This ensures the app always works — with or without Alpaca API keys.
/// </summary>
public class CompositeMarketDataService : IMarketDataService
{
    private readonly AlpacaMarketDataService _alpaca;
    private readonly SampleMarketDataService _sample;
    private readonly ILogger<CompositeMarketDataService> _logger;
    private bool? _useAlpaca;

    public string DataSource => _useAlpaca == true ? _alpaca.DataSource : _sample.DataSource;

    public CompositeMarketDataService(
        AlpacaMarketDataService alpaca,
        SampleMarketDataService sample,
        ILogger<CompositeMarketDataService> logger)
    {
        _alpaca = alpaca;
        _sample = sample;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync()
    {
        _useAlpaca = await _alpaca.IsAvailableAsync();
        return true; // Composite is always available (sample fallback)
    }

    public async Task<List<Candle>> GetCandlesAsync(string symbol, int count, string timeframe)
    {
        // Determine which source to use (cached after first check)
        if (!_useAlpaca.HasValue)
            _useAlpaca = await _alpaca.IsAvailableAsync();

        if (_useAlpaca.Value)
        {
            try
            {
                var candles = await _alpaca.GetCandlesAsync(symbol, count, timeframe);
                if (candles.Count > 0) return candles;

                // Alpaca returned empty — might be invalid symbol or no data
                _logger.LogWarning("Alpaca returned 0 bars for {Symbol} {Timeframe} — falling back to sample data",
                    symbol, timeframe);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Alpaca failed for {Symbol} {Timeframe} — falling back to sample data",
                    symbol, timeframe);
            }
        }

        // Fallback to sample data
        return await _sample.GetCandlesAsync(symbol, count, timeframe);
    }
}