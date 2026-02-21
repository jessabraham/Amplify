using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Enumerations;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

public class TradeSignal : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Signal details
    public string Asset { get; set; } = string.Empty;
    public AssetClass AssetClass { get; set; }
    public SignalType SignalType { get; set; }
    public MarketRegime Regime { get; set; }

    // Source & Status
    public SignalSource Source { get; set; } = SignalSource.Manual;
    public SignalStatus Status { get; set; } = SignalStatus.Pending;

    // Pattern context (from scanner)
    public string? PatternName { get; set; }
    public string? PatternTimeframe { get; set; }
    public decimal? PatternConfidence { get; set; }

    // Scoring & risk
    public decimal SetupScore { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal Target2 { get; set; }
    public decimal RiskPercent { get; set; }

    // AI advisory
    public string? AIAdvisoryJson { get; set; }
    public string? AISummary { get; set; }
    public string? AIBias { get; set; }
    public decimal? AIConfidence { get; set; }
    public string? AIRecommendedAction { get; set; }

    // Risk assessment (saved from Risk Engine)
    public int? RiskShareCount { get; set; }
    public decimal? RiskPositionValue { get; set; }
    public decimal? RiskMaxLoss { get; set; }
    public decimal? RiskRewardRatio { get; set; }
    public decimal? RiskKellyPercent { get; set; }
    public bool? RiskPassesCheck { get; set; }
    public decimal? RiskPortfolioSize { get; set; }
    public string? RiskWarnings { get; set; }

    // Status tracking
    public bool IsActive { get; set; } = true;
    public DateTime? ArchivedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime? RejectedAt { get; set; }

    // Relationships
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}