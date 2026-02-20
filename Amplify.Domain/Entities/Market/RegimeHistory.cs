using Amplify.Domain.Enumerations;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Market;

public class RegimeHistory : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Symbol { get; set; } = string.Empty;
    public MarketRegime Regime { get; set; }
    public decimal Confidence { get; set; }
    public DateTime DetectedAt { get; set; }
    public string? FeatureVectorJson { get; set; }
}