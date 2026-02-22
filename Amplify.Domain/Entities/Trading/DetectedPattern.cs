using Amplify.Domain.Enumerations;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Trading;

public class DetectedPattern : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // What was detected
    public string Asset { get; set; } = string.Empty;
    public PatternType PatternType { get; set; }
    public PatternDirection Direction { get; set; }
    public PatternTimeframe Timeframe { get; set; }

    // Quality metrics
    public decimal Confidence { get; set; }       // 0-100 how clean the pattern is
    public decimal HistoricalWinRate { get; set; } // historical success rate of this pattern
    public string Description { get; set; } = string.Empty; // human-readable explanation

    // Price context at detection
    public decimal DetectedAtPrice { get; set; }
    public decimal SuggestedEntry { get; set; }
    public decimal SuggestedStop { get; set; }
    public decimal SuggestedTarget { get; set; }

    // Chart marker data
    public DateTime PatternStartDate { get; set; }
    public DateTime PatternEndDate { get; set; }

    // AI analysis
    public string? AIAnalysis { get; set; }  // Ollama's take on the pattern
    public decimal? AIConfidence { get; set; } // AI's confidence score

    // ═══════════════════════════════════════════════════════════════
    // LIFECYCLE — pattern validity and outcome tracking
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Current lifecycle status of this pattern.</summary>
    public PatternStatus Status { get; set; } = PatternStatus.Active;

    /// <summary>When this pattern expires based on its timeframe.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Current price at last lifecycle check.</summary>
    public decimal? CurrentPrice { get; set; }

    /// <summary>High water mark — furthest price moved in pattern direction since detection.</summary>
    public decimal? HighWaterMark { get; set; }

    /// <summary>Low water mark — furthest price moved against pattern direction since detection.</summary>
    public decimal? LowWaterMark { get; set; }

    /// <summary>When status changed to a terminal state (HitTarget/HitStop/Expired).</summary>
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Price at resolution (target hit price, stop hit price, or last price at expiry).</summary>
    public decimal? ResolutionPrice { get; set; }

    // Legacy outcome tracking (kept for backward compat)
    public bool? WasCorrect { get; set; }
    public decimal? ActualPnLPercent { get; set; }

    // Was a signal auto-generated?
    public Guid? GeneratedSignalId { get; set; }
    public bool EmailSent { get; set; }

    // Link to portfolio advice that referenced this pattern
    public Guid? PortfolioAdviceId { get; set; }

    // User
    public string UserId { get; set; } = string.Empty;

    // ═══════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Is this pattern still valid for trading decisions?</summary>
    public bool IsLive => Status == PatternStatus.Active || Status == PatternStatus.PlayingOut;

    /// <summary>Has this pattern reached a terminal state?</summary>
    public bool IsResolved => Status == PatternStatus.HitTarget || Status == PatternStatus.HitStop
        || Status == PatternStatus.Expired || Status == PatternStatus.Invalidated;

    /// <summary>Calculate expiry based on timeframe. Call at creation time.</summary>
    public static DateTime CalculateExpiry(PatternTimeframe timeframe, DateTime detectedAt) => timeframe switch
    {
        PatternTimeframe.OneMinute => detectedAt.AddMinutes(30),
        PatternTimeframe.FiveMinute => detectedAt.AddHours(2),
        PatternTimeframe.FifteenMinute => detectedAt.AddHours(6),
        PatternTimeframe.OneHour => detectedAt.AddHours(8),
        PatternTimeframe.FourHour => detectedAt.AddDays(2),
        PatternTimeframe.Daily => detectedAt.AddDays(5),
        PatternTimeframe.Weekly => detectedAt.AddDays(14),
        _ => detectedAt.AddDays(5)
    };
}