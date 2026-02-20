namespace Amplify.Domain.ValueObjects;

public record RiskParameters(
    decimal StopLoss,
    decimal Target1,
    decimal Target2,
    decimal RiskPercent);