using Amplify.Domain.Enumerations;

namespace Amplify.Application.Common.DTOs.Market;

public class RegimeHistoryDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public MarketRegime Regime { get; set; }
    public decimal Confidence { get; set; }
    public DateTime DetectedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}