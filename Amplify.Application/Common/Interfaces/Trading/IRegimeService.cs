using Amplify.Application.Common.DTOs.Market;
using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.Trading;

public interface IRegimeService
{
    /// <summary>
    /// Detect regime from raw candle data: compute features → classify → persist to history.
    /// </summary>
    Task<Result<RegimeResultDto>> DetectAndStoreAsync(string symbol, List<Candle> candles);

    /// <summary>
    /// Get regime detection history for a symbol.
    /// </summary>
    Task<Result<List<RegimeHistoryDto>>> GetHistoryAsync(string symbol, int days = 30);

    /// <summary>
    /// Get the most recent regime for a symbol.
    /// </summary>
    Task<Result<RegimeResultDto>> GetLatestRegimeAsync(string symbol);
}