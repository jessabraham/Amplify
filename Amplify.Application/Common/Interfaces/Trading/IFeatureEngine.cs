using Amplify.Application.Common.DTOs.Market;
using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.Trading;

public interface IFeatureEngine
{
    /// <summary>
    /// Compute all technical indicators from OHLCV candle data.
    /// Requires at least 50 candles for accurate SMA50.
    /// </summary>
    FeatureVectorDto ComputeFeatures(string symbol, List<Candle> candles);
}