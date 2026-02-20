using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Models;

namespace Amplify.Application.Common.Interfaces.Trading;

public interface ITradeSignalService
{
    Task<Result<List<TradeSignalDto>>> GetActiveSignalsAsync(string userId);
    Task<Result<TradeSignalDto>> GetSignalByIdAsync(Guid id);
    Task<Result<TradeSignalDto>> CreateSignalAsync(TradeSignalDto dto, string userId);
    Task<Result<bool>> ArchiveSignalAsync(Guid id);
}