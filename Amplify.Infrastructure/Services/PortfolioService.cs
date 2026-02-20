using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amplify.Infrastructure.Services;

public class PortfolioService : IPortfolioService
{
    private readonly ApplicationDbContext _context;

    public PortfolioService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ── Positions ───────────────────────────────────────────────────

    public async Task<Result<List<PositionDto>>> GetOpenPositionsAsync(string userId)
    {
        var positions = await _context.Positions
            .Where(p => p.UserId == userId && p.Status == PositionStatus.Open)
            .OrderByDescending(p => p.EntryDateUtc)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return Result<List<PositionDto>>.Success(positions);
    }

    public async Task<Result<List<PositionDto>>> GetClosedPositionsAsync(string userId)
    {
        var positions = await _context.Positions
            .Where(p => p.UserId == userId && p.Status == PositionStatus.Closed)
            .OrderByDescending(p => p.ExitDateUtc)
            .Select(p => MapToDto(p))
            .ToListAsync();

        return Result<List<PositionDto>>.Success(positions);
    }

    public async Task<Result<PositionDto>> GetPositionByIdAsync(Guid id)
    {
        var p = await _context.Positions.FindAsync(id);
        if (p is null)
            return Result<PositionDto>.Failure("Position not found");

        return Result<PositionDto>.Success(MapToDto(p));
    }

    public async Task<Result<PositionDto>> OpenPositionAsync(PositionDto dto, string userId)
    {
        var position = new Position
        {
            Symbol = dto.Symbol,
            AssetClass = dto.AssetClass,
            SignalType = dto.SignalType,
            EntryPrice = dto.EntryPrice,
            Quantity = dto.Quantity,
            EntryDateUtc = DateTime.UtcNow,
            StopLoss = dto.StopLoss,
            Target1 = dto.Target1,
            Target2 = dto.Target2,
            CurrentPrice = dto.EntryPrice,  // starts at entry
            UnrealizedPnL = 0,
            RealizedPnL = 0,
            Status = PositionStatus.Open,
            Notes = dto.Notes,
            TradeSignalId = dto.TradeSignalId,
            UserId = userId
        };

        _context.Positions.Add(position);
        await _context.SaveChangesAsync();

        return Result<PositionDto>.Success(MapToDto(position));
    }

    public async Task<Result<PositionDto>> ClosePositionAsync(ClosePositionDto dto, string userId)
    {
        var position = await _context.Positions.FindAsync(dto.PositionId);

        if (position is null)
            return Result<PositionDto>.Failure("Position not found");

        if (position.UserId != userId)
            return Result<PositionDto>.Failure("Not authorized");

        if (position.Status != PositionStatus.Open)
            return Result<PositionDto>.Failure("Position is already closed");

        position.ExitPrice = dto.ExitPrice;
        position.ExitDateUtc = DateTime.UtcNow;
        position.CurrentPrice = dto.ExitPrice;
        position.Status = PositionStatus.Closed;
        position.IsActive = false;
        position.DeactivatedAt = DateTime.UtcNow;
        position.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(dto.Notes))
            position.Notes = dto.Notes;

        // Calculate realized P&L
        var multiplier = position.SignalType == SignalType.Short ? -1m : 1m;
        position.RealizedPnL = (dto.ExitPrice - position.EntryPrice) * position.Quantity * multiplier;
        position.UnrealizedPnL = 0;

        if (position.EntryPrice != 0)
            position.ReturnPercent = (dto.ExitPrice - position.EntryPrice) / position.EntryPrice * 100m * multiplier;

        await _context.SaveChangesAsync();

        return Result<PositionDto>.Success(MapToDto(position));
    }

    public async Task<Result<PositionDto>> UpdateCurrentPriceAsync(Guid positionId, decimal currentPrice)
    {
        var position = await _context.Positions.FindAsync(positionId);

        if (position is null)
            return Result<PositionDto>.Failure("Position not found");

        if (position.Status != PositionStatus.Open)
            return Result<PositionDto>.Failure("Cannot update price on closed position");

        position.CurrentPrice = currentPrice;
        position.UpdatedAt = DateTime.UtcNow;

        var multiplier = position.SignalType == SignalType.Short ? -1m : 1m;
        position.UnrealizedPnL = (currentPrice - position.EntryPrice) * position.Quantity * multiplier;

        if (position.EntryPrice != 0)
            position.ReturnPercent = (currentPrice - position.EntryPrice) / position.EntryPrice * 100m * multiplier;

        await _context.SaveChangesAsync();

        return Result<PositionDto>.Success(MapToDto(position));
    }

    // ── Portfolio Summary ───────────────────────────────────────────

    public async Task<Result<PortfolioSummaryDto>> GetPortfolioSummaryAsync(string userId)
    {
        var openPositions = await _context.Positions
            .Where(p => p.UserId == userId && p.Status == PositionStatus.Open)
            .ToListAsync();

        var closedCount = await _context.Positions
            .CountAsync(p => p.UserId == userId && p.Status == PositionStatus.Closed);

        var investedAmount = openPositions.Sum(p => p.EntryPrice * p.Quantity);
        var unrealizedPnL = openPositions.Sum(p => p.UnrealizedPnL);
        var realizedPnL = await _context.Positions
            .Where(p => p.UserId == userId && p.Status == PositionStatus.Closed)
            .SumAsync(p => p.RealizedPnL);

        // Daily P&L: sum of unrealized changes for positions opened before today
        // plus realized P&L from positions closed today
        var todayStart = DateTime.UtcNow.Date;
        var closedToday = await _context.Positions
            .Where(p => p.UserId == userId
                && p.Status == PositionStatus.Closed
                && p.ExitDateUtc >= todayStart)
            .SumAsync(p => p.RealizedPnL);
        var dailyPnL = unrealizedPnL + closedToday;

        // TODO: CashBalance should come from a user settings/account table.
        // For now default to 0 — user can set via portfolio config later.
        var cashBalance = 0m;
        var totalValue = cashBalance + investedAmount + unrealizedPnL;
        var riskExposure = totalValue > 0 ? investedAmount / totalValue * 100m : 0m;

        // Asset allocation
        var allocation = openPositions
            .GroupBy(p => p.AssetClass)
            .Select(g =>
            {
                var marketValue = g.Sum(p => p.CurrentPrice * p.Quantity);
                return new AssetAllocationDto
                {
                    AssetClass = g.Key.ToString(),
                    MarketValue = marketValue,
                    Percentage = investedAmount > 0 ? marketValue / investedAmount * 100m : 0m,
                    PositionCount = g.Count()
                };
            })
            .ToList();

        var summary = new PortfolioSummaryDto
        {
            TotalValue = totalValue,
            CashBalance = cashBalance,
            InvestedAmount = investedAmount,
            UnrealizedPnL = unrealizedPnL,
            RealizedPnL = realizedPnL,
            DailyPnL = dailyPnL,
            RiskExposurePercent = Math.Round(riskExposure, 2),
            OpenPositionCount = openPositions.Count,
            ClosedPositionCount = closedCount,
            OpenPositions = openPositions.Select(MapToDto).ToList(),
            AssetAllocation = allocation
        };

        return Result<PortfolioSummaryDto>.Success(summary);
    }

    // ── Snapshots ───────────────────────────────────────────────────

    public async Task<Result<bool>> TakeSnapshotAsync(string userId)
    {
        var summaryResult = await GetPortfolioSummaryAsync(userId);
        if (!summaryResult.IsSuccess)
            return Result<bool>.Failure(summaryResult.Error!);

        var s = summaryResult.Value!;

        var snapshot = new PortfolioSnapshot
        {
            UserId = userId,
            TotalValue = s.TotalValue,
            CashBalance = s.CashBalance,
            InvestedAmount = s.InvestedAmount,
            DailyPnL = s.DailyPnL,
            UnrealizedPnL = s.UnrealizedPnL,
            RealizedPnL = s.RealizedPnL,
            OpenPositions = s.OpenPositionCount,
            RiskExposurePercent = s.RiskExposurePercent
        };

        _context.PortfolioSnapshots.Add(snapshot);
        await _context.SaveChangesAsync();

        return Result<bool>.Success(true);
    }

    public async Task<Result<List<PortfolioSnapshotDto>>> GetSnapshotsAsync(string userId, int days = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var snapshots = await _context.PortfolioSnapshots
            .Where(s => s.UserId == userId && s.CreatedAt >= cutoff)
            .OrderBy(s => s.CreatedAt)
            .Select(s => new PortfolioSnapshotDto
            {
                Id = s.Id,
                TotalValue = s.TotalValue,
                CashBalance = s.CashBalance,
                InvestedAmount = s.InvestedAmount,
                DailyPnL = s.DailyPnL,
                UnrealizedPnL = s.UnrealizedPnL,
                RealizedPnL = s.RealizedPnL,
                OpenPositions = s.OpenPositions,
                RiskExposurePercent = s.RiskExposurePercent,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return Result<List<PortfolioSnapshotDto>>.Success(snapshots);
    }

    // ── Mapping ─────────────────────────────────────────────────────

    private static PositionDto MapToDto(Position p) => new()
    {
        Id = p.Id,
        Symbol = p.Symbol,
        AssetClass = p.AssetClass,
        SignalType = p.SignalType,
        EntryPrice = p.EntryPrice,
        Quantity = p.Quantity,
        EntryDateUtc = p.EntryDateUtc,
        ExitPrice = p.ExitPrice,
        ExitDateUtc = p.ExitDateUtc,
        StopLoss = p.StopLoss,
        Target1 = p.Target1,
        Target2 = p.Target2,
        CurrentPrice = p.CurrentPrice,
        UnrealizedPnL = p.UnrealizedPnL,
        RealizedPnL = p.RealizedPnL,
        ReturnPercent = p.ReturnPercent,
        Status = p.Status,
        Notes = p.Notes,
        TradeSignalId = p.TradeSignalId,
        CreatedAt = p.CreatedAt
    };
}