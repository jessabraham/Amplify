using Amplify.Domain.Enumerations;
using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

public class UserOverride : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid TradeSignalId { get; set; }
    public TradeSignal TradeSignal { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public OverrideType OverrideType { get; set; }
    public OverrideReason Reason { get; set; }
    public string? Notes { get; set; }

    // What the user changed (if Modified)
    public decimal? ModifiedEntryPrice { get; set; }
    public decimal? ModifiedStopLoss { get; set; }
    public decimal? ModifiedTarget1 { get; set; }
    public decimal? ModifiedTarget2 { get; set; }

    // Outcome tracking
    public decimal? ActualPnL { get; set; }
    public bool? WasCorrect { get; set; }
}