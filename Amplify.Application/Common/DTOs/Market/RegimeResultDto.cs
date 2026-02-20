using Amplify.Domain.Enumerations;

namespace Amplify.Application.Common.DTOs.Market;

public class RegimeResultDto
{
    public string Symbol { get; set; } = string.Empty;
    public MarketRegime Regime { get; set; }
    public decimal Confidence { get; set; }
    public string Rationale { get; set; } = string.Empty;
    public FeatureVectorDto? Features { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}