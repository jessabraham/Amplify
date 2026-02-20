namespace Amplify.Application.Common.DTOs.Trading;

/// <summary>
/// DTO sent to clients via SignalR when a background scan completes.
/// Lives in Application so both Infrastructure (background service) and API (hub) can reference it.
/// </summary>
public class ScanNotification
{
    public string Symbol { get; set; } = "";
    public int PatternCount { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? OverallBias { get; set; }
    public decimal? AIConfidence { get; set; }
    public string? RecommendedAction { get; set; }
    public string? TopPattern { get; set; }
    public decimal? TopPatternConfidence { get; set; }
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// High-priority alert: high-confidence pattern with strong AI confirmation.
    /// </summary>
    public bool IsAlert { get; set; }
    public string? AlertMessage { get; set; }
}