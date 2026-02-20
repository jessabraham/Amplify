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

    // Scoring & risk
    public decimal SetupScore { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal Target2 { get; set; }
    public decimal RiskPercent { get; set; }

    // AI advisory
    public string? AIAdvisoryJson { get; set; }

    // Status
    public bool IsActive { get; set; } = true;
    public DateTime? ArchivedAt { get; set; }

    // Relationships
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}