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
    public string? AISummary { get; set; }
    public string? AIBias { get; set; }
    public decimal? AIConfidence { get; set; }
    public string? AIRecommendedAction { get; set; }

    // Risk assessment
    public int? RiskShareCount { get; set; }
    public decimal? RiskPositionValue { get; set; }
    public decimal? RiskMaxLoss { get; set; }
    public decimal? RiskRewardRatio { get; set; }
    public decimal? RiskKellyPercent { get; set; }
    public bool? RiskPassesCheck { get; set; }
    public decimal? RiskPortfolioSize { get; set; }
    public string? RiskWarnings { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}