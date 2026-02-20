using Amplify.Domain.Enumerations;

namespace Amplify.Application.Common.Models;

/// <summary>
/// Result returned when the pattern engine detects a pattern.
/// </summary>
public class PatternResult
{
    public PatternType PatternType { get; set; }
    public PatternDirection Direction { get; set; }
    public decimal Confidence { get; set; }          // 0-100
    public decimal HistoricalWinRate { get; set; }   // typical win rate for this pattern
    public string Description { get; set; } = "";
    public string PatternName { get; set; } = "";
    public string Timeframe { get; set; } = "Daily"; // 4H, Daily, Weekly

    // Suggested trade levels
    public decimal SuggestedEntry { get; set; }
    public decimal SuggestedStop { get; set; }
    public decimal SuggestedTarget { get; set; }

    // Where the pattern was found
    public int StartIndex { get; set; }
    public int EndIndex { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}