namespace Amplify.Domain.Exceptions;

public class RiskThresholdExceededException : DomainException
{
    public RiskThresholdExceededException(decimal riskPercent)
        : base($"Risk of {riskPercent}% exceeds the maximum allowed threshold.") { }
}