using Amplify.Domain.Enumerations;
using Amplify.Domain.Interfaces.Common;

namespace Amplify.Domain.Entities.Market;

public class MarketTick : IEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string Symbol { get; set; } = string.Empty;
    public AssetClass AssetClass { get; set; }

    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }

    public DateTime Timestamp { get; set; }
}