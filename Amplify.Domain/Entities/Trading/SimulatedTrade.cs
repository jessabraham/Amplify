using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Enumerations;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

/// <summary>
/// Tracks a simulated (paper) trade from signal creation through resolution.
/// Every TradeSignal gets a SimulatedTrade. The simulation engine updates
/// this entity as price moves, eventually resolving it when target or stop is hit.
/// </summary>
public class SimulatedTrade : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Links back to the signal that created this trade
    public Guid? TradeSignalId { get; set; }
    public TradeSignal? TradeSignal { get; set; }

    // Links to the pattern that was selected (if from scanner)
    public Guid? DetectedPatternId { get; set; }

    // Trade setup (copied from signal for historical record)
    public string Asset { get; set; } = string.Empty;
    public SignalType Direction { get; set; }           // Long or Short
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal? Target2 { get; set; }

    // Context at entry (for learning)
    public MarketRegime RegimeAtEntry { get; set; }
    public PatternType? PatternType { get; set; }
    public PatternDirection? PatternDirection { get; set; }
    public string? PatternTimeframe { get; set; }       // "4H", "Daily", "Weekly"
    public decimal? PatternConfidence { get; set; }
    public decimal? AIConfidence { get; set; }
    public string? AIRecommendedAction { get; set; }
    public string? TimeframeAlignment { get; set; }     // "All Bullish", "Conflicting", etc.
    public string? RegimeAlignment { get; set; }        // "Aligned", "Mixed", "Conflicting"
    public string? MAAlignment { get; set; }            // "Bullish Stack", etc.
    public string? VolumeProfile { get; set; }          // "Breakout", "Normal", etc.
    public decimal? RSIAtEntry { get; set; }

    // Position sizing
    public int? ShareCount { get; set; }
    public decimal? PositionValue { get; set; }
    public decimal? MaxRisk { get; set; }               // $ amount at risk

    // Simulation tracking
    public SimulationStatus Status { get; set; } = SimulationStatus.Pending;
    public TradeOutcome Outcome { get; set; } = TradeOutcome.Open;
    public DateTime? ActivatedAt { get; set; }          // When simulation started tracking
    public DateTime? ResolvedAt { get; set; }           // When outcome was determined
    public int DaysHeld { get; set; }                   // Trading days from entry to resolution
    public int MaxExpirationDays { get; set; } = 30;    // Auto-expire after this many days

    // Price tracking during simulation
    public decimal? HighestPriceSeen { get; set; }      // Max favorable excursion
    public decimal? LowestPriceSeen { get; set; }       // Max adverse excursion
    public decimal? ExitPrice { get; set; }             // Where the trade actually closed

    // P&L
    public decimal? PnLDollars { get; set; }            // Actual dollar P&L
    public decimal? PnLPercent { get; set; }            // Percentage P&L
    public decimal? RMultiple { get; set; }             // P&L divided by risk (1R, 2R, -1R etc.)
    public decimal? MaxDrawdownPercent { get; set; }    // Worst point during the trade

    // User
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}