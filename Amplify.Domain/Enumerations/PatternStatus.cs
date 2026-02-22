namespace Amplify.Domain.Enumerations;

/// <summary>
/// Lifecycle status of a detected pattern.
/// Patterns move through: Active → PlayingOut → (HitTarget | HitStop | Expired)
/// </summary>
public enum PatternStatus
{
    /// <summary>Pattern just detected, within its validity window.</summary>
    Active,

    /// <summary>Price has moved in the pattern's direction but hasn't hit target or stop yet.</summary>
    PlayingOut,

    /// <summary>Price reached the suggested target. Pattern was correct.</summary>
    HitTarget,

    /// <summary>Price hit the suggested stop loss. Pattern failed.</summary>
    HitStop,

    /// <summary>Validity window expired without price hitting target or stop.</summary>
    Expired,

    /// <summary>Pattern was superseded by a newer contradictory pattern on the same symbol/timeframe.</summary>
    Invalidated
}