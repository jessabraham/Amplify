using Amplify.Domain.Enumerations;

namespace Amplify.Application.Common.DTOs.Trading;

public class TradeSignalDto
{
    public Guid Id { get; set; }
    public string Asset { get; set; } = string.Empty;
    public AssetClass AssetClass { get; set; }
    public SignalType SignalType { get; set; }
    public MarketRegime Regime { get; set; }
    public decimal SetupScore { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal Target2 { get; set; }
    public decimal RiskPercent { get; set; }
    public string? AIAdvisoryJson { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}