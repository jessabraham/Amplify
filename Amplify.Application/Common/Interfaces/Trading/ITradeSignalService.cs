using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Models;
using Amplify.Domain.Enumerations;

namespace Amplify.Application.Common.Interfaces.Trading;

public interface ITradeSignalService
{
    Task<Result<List<TradeSignalDto>>> GetSignalsAsync(string userId, SignalSource? source = null, SignalStatus? status = null);
    Task<Result<TradeSignalDto>> GetSignalByIdAsync(Guid id);
    Task<Result<TradeSignalDto>> CreateSignalAsync(TradeSignalDto dto, string userId);
    Task<Result<bool>> AcceptSignalAsync(Guid id, string userId);
    Task<Result<bool>> RejectSignalAsync(Guid id);
    Task<Result<bool>> ArchiveSignalAsync(Guid id);
    Task<Result<bool>> DeleteSignalAsync(Guid id, string userId);
    Task<Result<int>> ClearAllSignalsAsync(string userId);
    Task<Result<SignalStatsDto>> GetSignalStatsAsync(string userId);
}

public class SignalStatsDto
{
    public int TotalSignals { get; set; }
    public int AISignals { get; set; }
    public int ManualSignals { get; set; }
    public int Pending { get; set; }
    public int Accepted { get; set; }
    public int Rejected { get; set; }

    // Performance (from linked simulated trades)
    public decimal AIWinRate { get; set; }
    public decimal ManualWinRate { get; set; }
    public decimal AIAvgRMultiple { get; set; }
    public decimal ManualAvgRMultiple { get; set; }
    public int AITradeCount { get; set; }
    public int ManualTradeCount { get; set; }
}