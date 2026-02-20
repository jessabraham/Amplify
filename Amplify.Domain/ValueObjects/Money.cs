namespace Amplify.Domain.ValueObjects;

public record Money(decimal Amount, string Currency = "USD")
{
    public static Money Zero => new(0m);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}");
        return new Money(Amount + other.Amount, Currency);
    }
}