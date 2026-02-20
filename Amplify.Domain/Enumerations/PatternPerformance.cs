using Amplify.Domain.Enumerations;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

/// <summary>
/// Aggregated performance stats for a specific pattern + timeframe + regime combination.
/// Updated after each trade resolves. The AI reads this to give data-backed recommendations.
/// </summary>
public class PatternPerformance : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // The combination being tracked
    public PatternType PatternType { get; set; }
    public PatternDirection Direction { get; set; }
    public string Timeframe { get; set; } = "Daily";    // "4H", "Daily", "Weekly"
    public MarketRegime Regime { get; set; }

    // Win/Loss counts
    public int TotalTrades { get; set; }
    public int Wins { get; set; }                        // Hit target
    public int Losses { get; set; }                      // Hit stop
    public int Expired { get; set; }                     // Timed out
    public decimal WinRate { get; set; }                 // Wins / (Wins + Losses) * 100

    // P&L stats
    public decimal AvgWinPercent { get; set; }           // Average winning trade %
    public decimal AvgLossPercent { get; set; }          // Average losing trade %
    public decimal AvgRMultiple { get; set; }            // Average R-multiple
    public decimal BestTradePercent { get; set; }        // Best single trade
    public decimal WorstTradePercent { get; set; }       // Worst single trade
    public decimal TotalPnLPercent { get; set; }         // Sum of all trade %s
    public decimal ProfitFactor { get; set; }            // Gross wins / Gross losses

    // Context performance (how does this pattern do with these conditions?)
    public decimal WinRateWhenAligned { get; set; }      // TF alignment = All same direction
    public int TradesWhenAligned { get; set; }
    public decimal WinRateWhenConflicting { get; set; }  // TF alignment = Conflicting
    public int TradesWhenConflicting { get; set; }
    public decimal WinRateWithBreakoutVol { get; set; }  // Volume = Breakout
    public int TradesWithBreakoutVol { get; set; }

    // Timing
    public decimal AvgDaysHeld { get; set; }             // How long winning trades take
    public DateTime? LastTradeDate { get; set; }

    // Per-user (each user has their own stats)
    public string UserId { get; set; } = string.Empty;
}