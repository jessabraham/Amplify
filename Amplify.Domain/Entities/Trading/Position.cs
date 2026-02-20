using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Enumerations;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

public class Position : IEntity, ISoftDeletable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Asset info
    public string Symbol { get; set; } = string.Empty;
    public AssetClass AssetClass { get; set; }
    public SignalType SignalType { get; set; }

    // Entry
    public decimal EntryPrice { get; set; }
    public decimal Quantity { get; set; }
    public DateTime EntryDateUtc { get; set; } = DateTime.UtcNow;

    // Exit (populated when closed)
    public decimal? ExitPrice { get; set; }
    public DateTime? ExitDateUtc { get; set; }

    // Risk levels
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal? Target2 { get; set; }

    // Current market price (updated periodically)
    public decimal CurrentPrice { get; set; }

    // P&L
    public decimal UnrealizedPnL { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal? ReturnPercent { get; set; }

    // Status
    public PositionStatus Status { get; set; } = PositionStatus.Open;
    public bool IsActive { get; set; } = true;
    public DateTime? DeactivatedAt { get; set; }

    // Optional notes
    public string? Notes { get; set; }

    // Link to originating signal (optional — positions can exist without signals)
    public Guid? TradeSignalId { get; set; }
    public TradeSignal? TradeSignal { get; set; }

    // User ownership
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}