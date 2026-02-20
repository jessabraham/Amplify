using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

/// <summary>
/// A symbol on a user's watchlist for background pattern scanning.
/// </summary>
public class WatchlistItem : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Symbol { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool EnableAI { get; set; } = true;
    public decimal MinConfidence { get; set; } = 60;

    /// <summary>
    /// Scan interval in minutes. Default: 30.
    /// </summary>
    public int ScanIntervalMinutes { get; set; } = 30;

    public DateTime? LastScannedAt { get; set; }
    public int LastPatternCount { get; set; }
    public string? LastBias { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }
}