using Amplify.Domain.Exceptions;

namespace Amplify.Domain.ValueObjects;

public record PortfolioSize
{
    public decimal Value { get; }

    public PortfolioSize(decimal value)
    {
        if (value < 1_000m || value > 10_000_000m)
            throw new DomainException("Portfolio size must be between $1,000 and $10,000,000.");
        Value = value;
    }
}