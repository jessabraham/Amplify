using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.Trading;

public interface IPortfolioService
{
    // Positions
    Task<Result<List<PositionDto>>> GetOpenPositionsAsync(string userId);
    Task<Result<List<PositionDto>>> GetClosedPositionsAsync(string userId);
    Task<Result<PositionDto>> GetPositionByIdAsync(Guid id);
    Task<Result<PositionDto>> OpenPositionAsync(PositionDto dto, string userId);
    Task<Result<PositionDto>> ClosePositionAsync(ClosePositionDto dto, string userId);
    Task<Result<PositionDto>> UpdateCurrentPriceAsync(Guid positionId, decimal currentPrice);

    // Portfolio summary
    Task<Result<PortfolioSummaryDto>> GetPortfolioSummaryAsync(string userId);

    // Snapshots
    Task<Result<bool>> TakeSnapshotAsync(string userId);
    Task<Result<List<PortfolioSnapshotDto>>> GetSnapshotsAsync(string userId, int days = 30);
}