namespace Amplify.Domain.Exceptions;

public class SignalNotFoundException : DomainException
{
    public SignalNotFoundException(Guid id)
        : base($"Trade signal with ID '{id}' was not found.") { }
}