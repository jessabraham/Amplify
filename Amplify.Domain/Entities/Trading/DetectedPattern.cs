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

    // Outcome tracking (filled after trade resolves)
    public bool? WasCorrect { get; set; }
    public decimal? ActualPnLPercent { get; set; }
    public DateTime? ResolvedAt { get; set; }

    // Was a signal auto-generated?
    public Guid? GeneratedSignalId { get; set; }
    public bool EmailSent { get; set; }

    // User
    public string UserId { get; set; } = string.Empty;
}