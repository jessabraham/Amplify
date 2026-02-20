using Amplify.Application.Common.DTOs.Market;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Amplify.Domain.Entities.Market;
using Amplify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Amplify.Infrastructure.Services;

public class RegimeService : IRegimeService
{
    private readonly ApplicationDbContext _context;
    private readonly IFeatureEngine _featureEngine;
    private readonly IRegimeEngine _regimeEngine;

    public RegimeService(
        ApplicationDbContext context,
        IFeatureEngine featureEngine,
        IRegimeEngine regimeEngine)
    {
        _context = context;
        _featureEngine = featureEngine;
        _regimeEngine = regimeEngine;
    }

    public async Task<Result<RegimeResultDto>> DetectAndStoreAsync(string symbol, List<Candle> candles)
    {
        if (candles.Count < 50)
            return Result<RegimeResultDto>.Failure("Need at least 50 candles for regime detection.");

        // 1. Compute features
        FeatureVectorDto features;
        try
        {
            features = _featureEngine.ComputeFeatures(symbol, candles);
        }
        catch (Exception ex)
        {
            return Result<RegimeResultDto>.Failure($"Feature computation failed: {ex.Message}");
        }

        // 2. Classify regime
        var result = _regimeEngine.DetectRegime(features);

        // 3. Persist feature vector
        var featureEntity = new FeatureVector
        {
            Symbol = symbol,
            RSI = features.RSI,
            MACD = features.MACD,
            MACDSignal = features.MACDSignal,
            BollingerUpper = features.BollingerUpper,
            BollingerLower = features.BollingerLower,
            ATR = features.ATR,
            SMA20 = features.SMA20,
            SMA50 = features.SMA50,
            EMA12 = features.EMA12,
            EMA26 = features.EMA26,
            VWAP = features.VWAP,
            VolumeAvg20 = features.VolumeAvg20,
            CalculatedAt = features.CalculatedAt
        };
        _context.FeatureVectors.Add(featureEntity);

        // 4. Persist regime history
        var history = new RegimeHistory
        {
            Symbol = symbol,
            Regime = result.Regime,
            Confidence = result.Confidence,
            DetectedAt = DateTime.UtcNow,
            FeatureVectorJson = JsonSerializer.Serialize(features)
        };
        _context.RegimeHistory.Add(history);

        await _context.SaveChangesAsync();

        return Result<RegimeResultDto>.Success(result);
    }

    public async Task<Result<List<RegimeHistoryDto>>> GetHistoryAsync(string symbol, int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var history = await _context.RegimeHistory
            .Where(r => r.Symbol == symbol && r.CreatedAt >= cutoff)
            .OrderByDescending(r => r.DetectedAt)
            .Select(r => new RegimeHistoryDto
            {
                Id = r.Id,
                Symbol = r.Symbol,
                Regime = r.Regime,
                Confidence = r.Confidence,
                DetectedAt = r.DetectedAt,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Result<List<RegimeHistoryDto>>.Success(history);
    }

    public async Task<Result<RegimeResultDto>> GetLatestRegimeAsync(string symbol)
    {
        var latest = await _context.RegimeHistory
            .Where(r => r.Symbol == symbol)
            .OrderByDescending(r => r.DetectedAt)
            .FirstOrDefaultAsync();

        if (latest is null)
            return Result<RegimeResultDto>.Failure($"No regime history for {symbol}");

        FeatureVectorDto? features = null;
        if (!string.IsNullOrEmpty(latest.FeatureVectorJson))
        {
            try { features = JsonSerializer.Deserialize<FeatureVectorDto>(latest.FeatureVectorJson); }
            catch { /* swallow deserialization errors */ }
        }

        return Result<RegimeResultDto>.Success(new RegimeResultDto
        {
            Symbol = latest.Symbol,
            Regime = latest.Regime,
            Confidence = latest.Confidence,
            Rationale = "Retrieved from history",
            Features = features,
            DetectedAt = latest.DetectedAt
        });
    }
}