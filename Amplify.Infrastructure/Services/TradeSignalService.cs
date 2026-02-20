using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Amplify.Domain.Entities.Trading;
using Amplify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amplify.Infrastructure.Services;

public class TradeSignalService : ITradeSignalService
{
    private readonly ApplicationDbContext _context;

    public TradeSignalService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<TradeSignalDto>>> GetActiveSignalsAsync(string userId)
    {
        var signals = await _context.TradeSignals
            .Where(s => s.UserId == userId && s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new TradeSignalDto
            {
                Id = s.Id,
                Asset = s.Asset,
                AssetClass = s.AssetClass,
                SignalType = s.SignalType,
                Regime = s.Regime,
                SetupScore = s.SetupScore,
                EntryPrice = s.EntryPrice,
                StopLoss = s.StopLoss,
                Target1 = s.Target1,
                Target2 = s.Target2,
                RiskPercent = s.RiskPercent,
                AIAdvisoryJson = s.AIAdvisoryJson,
                AISummary = s.AISummary,
                AIBias = s.AIBias,
                AIConfidence = s.AIConfidence,
                AIRecommendedAction = s.AIRecommendedAction,
                RiskShareCount = s.RiskShareCount,
                RiskPositionValue = s.RiskPositionValue,
                RiskMaxLoss = s.RiskMaxLoss,
                RiskRewardRatio = s.RiskRewardRatio,
                RiskKellyPercent = s.RiskKellyPercent,
                RiskPassesCheck = s.RiskPassesCheck,
                RiskPortfolioSize = s.RiskPortfolioSize,
                RiskWarnings = s.RiskWarnings,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Result<List<TradeSignalDto>>.Success(signals);
    }

    public async Task<Result<TradeSignalDto>> GetSignalByIdAsync(Guid id)
    {
        var s = await _context.TradeSignals.FindAsync(id);

        if (s is null)
            return Result<TradeSignalDto>.Failure("Signal not found");

        return Result<TradeSignalDto>.Success(new TradeSignalDto
        {
            Id = s.Id,
            Asset = s.Asset,
            AssetClass = s.AssetClass,
            SignalType = s.SignalType,
            Regime = s.Regime,
            SetupScore = s.SetupScore,
            EntryPrice = s.EntryPrice,
            StopLoss = s.StopLoss,
            Target1 = s.Target1,
            Target2 = s.Target2,
            RiskPercent = s.RiskPercent,
            AIAdvisoryJson = s.AIAdvisoryJson,
            AISummary = s.AISummary,
            AIBias = s.AIBias,
            AIConfidence = s.AIConfidence,
            AIRecommendedAction = s.AIRecommendedAction,
            RiskShareCount = s.RiskShareCount,
            RiskPositionValue = s.RiskPositionValue,
            RiskMaxLoss = s.RiskMaxLoss,
            RiskRewardRatio = s.RiskRewardRatio,
            RiskKellyPercent = s.RiskKellyPercent,
            RiskPassesCheck = s.RiskPassesCheck,
            RiskPortfolioSize = s.RiskPortfolioSize,
            RiskWarnings = s.RiskWarnings,
            IsActive = s.IsActive,
            CreatedAt = s.CreatedAt
        });
    }

    public async Task<Result<TradeSignalDto>> CreateSignalAsync(TradeSignalDto dto, string userId)
    {
        var signal = new TradeSignal
        {
            Asset = dto.Asset,
            AssetClass = dto.AssetClass,
            SignalType = dto.SignalType,
            Regime = dto.Regime,
            SetupScore = dto.SetupScore,
            EntryPrice = dto.EntryPrice,
            StopLoss = dto.StopLoss,
            Target1 = dto.Target1,
            Target2 = dto.Target2,
            RiskPercent = dto.RiskPercent,
            AIAdvisoryJson = dto.AIAdvisoryJson,
            // AI fields
            AISummary = dto.AISummary,
            AIBias = dto.AIBias,
            AIConfidence = dto.AIConfidence,
            AIRecommendedAction = dto.AIRecommendedAction,
            // Risk fields
            RiskShareCount = dto.RiskShareCount,
            RiskPositionValue = dto.RiskPositionValue,
            RiskMaxLoss = dto.RiskMaxLoss,
            RiskRewardRatio = dto.RiskRewardRatio,
            RiskKellyPercent = dto.RiskKellyPercent,
            RiskPassesCheck = dto.RiskPassesCheck,
            RiskPortfolioSize = dto.RiskPortfolioSize,
            RiskWarnings = dto.RiskWarnings,
            UserId = userId
        };

        _context.TradeSignals.Add(signal);
        await _context.SaveChangesAsync();

        dto.Id = signal.Id;
        dto.IsActive = signal.IsActive;
        dto.CreatedAt = signal.CreatedAt;

        return Result<TradeSignalDto>.Success(dto);
    }

    public async Task<Result<bool>> ArchiveSignalAsync(Guid id)
    {
        var signal = await _context.TradeSignals.FindAsync(id);

        if (signal is null)
            return Result<bool>.Failure("Signal not found");

        signal.IsActive = false;
        signal.ArchivedAt = DateTime.UtcNow;
        signal.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }
}