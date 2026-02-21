using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.AI;

/// <summary>
/// Stores a snapshot of AI portfolio allocation advice.
/// Each record represents one "Get AI Allocation Advice" run.
/// </summary>
public class PortfolioAdvice : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Portfolio state at time of advice
    public decimal CashAvailable { get; set; }
    public decimal TotalInvested { get; set; }
    public int OpenPositionCount { get; set; }
    public int WatchlistCount { get; set; }

    // AI output
    public string Summary { get; set; } = string.Empty;
    public string DiversificationScore { get; set; } = string.Empty;
    public decimal TotalSuggestedAllocation { get; set; }
    public decimal CashRetained { get; set; }

    /// <summary>
    /// Full JSON of the AI response (allocations array, warnings, etc.)
    /// </summary>
    public string ResponseJson { get; set; } = string.Empty;

    /// <summary>
    /// How many of the suggested allocations were actually executed (tracked over time).
    /// </summary>
    public int AllocationsFollowed { get; set; }
    public int TotalAllocations { get; set; }

    // User ownership
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}