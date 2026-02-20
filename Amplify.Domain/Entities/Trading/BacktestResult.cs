using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Enumerations;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

public class BacktestResult : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public string Asset { get; set; } = string.Empty;
    public AssetClass AssetClass { get; set; }
    public SignalType SignalType { get; set; }
    public MarketRegime Regime { get; set; }

    // Backtest parameters
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }

    // Results
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal NetPnL { get; set; }
    public decimal SharpeRatio { get; set; }

    public string? ResultsJson { get; set; }
}