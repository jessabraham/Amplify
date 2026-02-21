using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.Market;

/// <summary>
/// Provides OHLCV candle data for pattern detection and charting.
/// Implementations may use Alpaca, Polygon, or generated sample data.
/// </summary>
public interface IMarketDataService
{
    /// <summary>
    /// Get historical candles for a symbol.
    /// </summary>
    /// <param name="symbol">Ticker symbol (e.g., "AAPL")</param>
    /// <param name="count">Number of bars to retrieve</param>
    /// <param name="timeframe">Bar size: "4H", "Daily", "Weekly"</param>
    /// <returns>List of candles ordered oldest-first</returns>
    Task<List<Candle>> GetCandlesAsync(string symbol, int count, string timeframe);

    /// <summary>
    /// Check if the market data provider is configured and available.
    /// Returns false if API keys are missing or invalid.
    /// </summary>
    Task<bool> IsAvailableAsync();

    /// <summary>
    /// Get the name of the active data source (e.g., "Alpaca", "Sample Data")
    /// </summary>
    string DataSource { get; }
}