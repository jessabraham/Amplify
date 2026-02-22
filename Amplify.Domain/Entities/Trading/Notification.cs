using Amplify.Domain.Entities.Identity;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

public class Notification : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Optional link to navigate to when clicked (e.g. "/portfolio", "/pattern-lifecycle")
    /// </summary>
    public string? LinkUrl { get; set; }

    /// <summary>
    /// Optional reference to the entity that triggered this notification
    /// </summary>
    public string? ReferenceId { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}

public enum NotificationType
{
    PatternDetected,
    PatternHitTarget,
    PatternHitStop,
    PatternExpired,
    AdvisorRan,
    PositionOpened,
    PositionClosed,
    PositionAutoClosedTarget,
    PositionAutoClosedStop,
    SystemAlert
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Urgent
}