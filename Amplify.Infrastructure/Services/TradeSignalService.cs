using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
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

    public async Task<Result<List<TradeSignalDto>>> GetSignalsAsync(
        string userId, SignalSource? source = null, SignalStatus? status = null)
    {
        var query = _context.TradeSignals
            .Where(s => s.UserId == userId && s.IsActive);

        if (source.HasValue)
            query = query.Where(s => s.Source == source.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        var entities = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var signals = entities.Select(MapToDto).ToList();

        return Result<List<TradeSignalDto>>.Success(signals);
    }

    public async Task<Result<TradeSignalDto>> GetSignalByIdAsync(Guid id)
    {
        var s = await _context.TradeSignals.FindAsync(id);
        if (s is null)
            return Result<TradeSignalDto>.Failure("Signal not found");

        return Result<TradeSignalDto>.Success(MapToDto(s));
    }

    public async Task<Result<TradeSignalDto>> CreateSignalAsync(TradeSignalDto dto, string userId)
    {
        var signal = new TradeSignal
        {
            Asset = dto.Asset,
            AssetClass = dto.AssetClass,
            SignalType = dto.SignalType,
            Regime = dto.Regime,
            Source = dto.Source,
            Status = dto.Source == SignalSource.Manual ? SignalStatus.Accepted : SignalStatus.Pending,
            PatternName = dto.PatternName,
            PatternTimeframe = dto.PatternTimeframe,
            PatternConfidence = dto.PatternConfidence,
            SetupScore = dto.SetupScore,
            EntryPrice = dto.EntryPrice,
            StopLoss = dto.StopLoss,
            Target1 = dto.Target1,
            Target2 = dto.Target2,
            RiskPercent = dto.RiskPercent,
            AIAdvisoryJson = dto.AIAdvisoryJson,
            AISummary = dto.AISummary,
            AIBias = dto.AIBias,
            AIConfidence = dto.AIConfidence,
            AIRecommendedAction = dto.AIRecommendedAction,
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

        // Manual signals are auto-accepted
        if (signal.Source == SignalSource.Manual)
            signal.AcceptedAt = DateTime.UtcNow;

        _context.TradeSignals.Add(signal);
        await _context.SaveChangesAsync();

        return Result<TradeSignalDto>.Success(MapToDto(signal));
    }

    public async Task<Result<bool>> AcceptSignalAsync(Guid id, string userId)
    {
        var signal = await _context.TradeSignals.FindAsync(id);
        if (signal is null)
            return Result<bool>.Failure("Signal not found");

        signal.Status = SignalStatus.Accepted;
        signal.AcceptedAt = DateTime.UtcNow;
        signal.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> RejectSignalAsync(Guid id)
    {
        var signal = await _context.TradeSignals.FindAsync(id);
        if (signal is null)
            return Result<bool>.Failure("Signal not found");

        signal.Status = SignalStatus.Rejected;
        signal.RejectedAt = DateTime.UtcNow;
        signal.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> ArchiveSignalAsync(Guid id)
    {
        var signal = await _context.TradeSignals.FindAsync(id);
        if (signal is null)
            return Result<bool>.Failure("Signal not found");

        signal.IsActive = false;
        signal.Status = SignalStatus.Archived;
        signal.ArchivedAt = DateTime.UtcNow;
        signal.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteSignalAsync(Guid id, string userId)
    {
        var signal = await _context.TradeSignals
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
        if (signal is null)
            return Result<bool>.Failure("Signal not found");

        // Remove linked simulated trades
        var simTrades = await _context.SimulatedTrades
            .Where(t => t.TradeSignalId == id).ToListAsync();
        _context.SimulatedTrades.RemoveRange(simTrades);

        // Remove linked overrides
        var overrides = await _context.UserOverrides
            .Where(o => o.TradeSignalId == id).ToListAsync();
        _context.UserOverrides.RemoveRange(overrides);

        _context.TradeSignals.Remove(signal);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<int>> ClearAllSignalsAsync(string userId)
    {
        var signals = await _context.TradeSignals
            .Where(s => s.UserId == userId)
            .ToListAsync();

        if (!signals.Any())
            return Result<int>.Success(0);

        var signalIds = signals.Select(s => s.Id).ToList();

        // Remove linked simulated trades
        var simTrades = await _context.SimulatedTrades
            .Where(t => t.TradeSignalId.HasValue && signalIds.Contains(t.TradeSignalId.Value))
            .ToListAsync();
        _context.SimulatedTrades.RemoveRange(simTrades);

        // Remove linked overrides
        var overrides = await _context.UserOverrides
            .Where(o => signalIds.Contains(o.TradeSignalId))
            .ToListAsync();
        _context.UserOverrides.RemoveRange(overrides);

        _context.TradeSignals.RemoveRange(signals);
        await _context.SaveChangesAsync();

        return Result<int>.Success(signals.Count);
    }

    public async Task<Result<SignalStatsDto>> GetSignalStatsAsync(string userId)
    {
        var signals = await _context.TradeSignals
            .Where(s => s.UserId == userId)
            .ToListAsync();

        // Get linked simulated trades for performance
        var simTrades = await _context.SimulatedTrades
            .Where(t => t.UserId == userId && t.Status == SimulationStatus.Resolved)
            .ToListAsync();

        // Split by source — match via TradeSignalId
        var aiSignalIds = signals.Where(s => s.Source == SignalSource.AI).Select(s => s.Id).ToHashSet();
        var manualSignalIds = signals.Where(s => s.Source == SignalSource.Manual).Select(s => s.Id).ToHashSet();

        var aiTrades = simTrades.Where(t => t.TradeSignalId.HasValue && aiSignalIds.Contains(t.TradeSignalId.Value)).ToList();
        var manualTrades = simTrades.Where(t => t.TradeSignalId.HasValue && manualSignalIds.Contains(t.TradeSignalId.Value)).ToList();

        var stats = new SignalStatsDto
        {
            TotalSignals = signals.Count,
            AISignals = signals.Count(s => s.Source == SignalSource.AI),
            ManualSignals = signals.Count(s => s.Source == SignalSource.Manual),
            Pending = signals.Count(s => s.Status == SignalStatus.Pending),
            Accepted = signals.Count(s => s.Status == SignalStatus.Accepted),
            Rejected = signals.Count(s => s.Status == SignalStatus.Rejected),

            AITradeCount = aiTrades.Count,
            ManualTradeCount = manualTrades.Count,
            AIWinRate = CalcWinRate(aiTrades),
            ManualWinRate = CalcWinRate(manualTrades),
            AIAvgRMultiple = aiTrades.Any(t => t.RMultiple.HasValue)
                ? aiTrades.Where(t => t.RMultiple.HasValue).Average(t => t.RMultiple!.Value) : 0,
            ManualAvgRMultiple = manualTrades.Any(t => t.RMultiple.HasValue)
                ? manualTrades.Where(t => t.RMultiple.HasValue).Average(t => t.RMultiple!.Value) : 0,
        };

        return Result<SignalStatsDto>.Success(stats);
    }

    private static decimal CalcWinRate(List<SimulatedTrade> trades)
    {
        var wins = trades.Count(t => t.Outcome == TradeOutcome.HitTarget1 || t.Outcome == TradeOutcome.HitTarget2);
        var losses = trades.Count(t => t.Outcome == TradeOutcome.HitStop);
        var total = wins + losses;
        return total > 0 ? (decimal)wins / total * 100 : 0;
    }

    private static TradeSignalDto MapToDto(TradeSignal s) => new()
    {
        Id = s.Id,
        Asset = s.Asset,
        AssetClass = s.AssetClass,
        SignalType = s.SignalType,
        Regime = s.Regime,
        Source = s.Source,
        Status = s.Status,
        PatternName = s.PatternName,
        PatternTimeframe = s.PatternTimeframe,
        PatternConfidence = s.PatternConfidence,
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
        CreatedAt = s.CreatedAt,
        AcceptedAt = s.AcceptedAt,
        RejectedAt = s.RejectedAt
    };
}