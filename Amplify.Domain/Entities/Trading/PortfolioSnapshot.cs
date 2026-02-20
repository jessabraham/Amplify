using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

public class PortfolioSnapshot : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public decimal TotalValue { get; set; }
    public decimal CashBalance { get; set; }
    public decimal InvestedAmount { get; set; }
    public decimal DailyPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }

    public int OpenPositions { get; set; }
    public decimal RiskExposurePercent { get; set; }

    public string? PositionsJson { get; set; }
}