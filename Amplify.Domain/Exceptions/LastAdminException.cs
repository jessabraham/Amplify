namespace Amplify.Domain.Exceptions;

public class LastAdminException : DomainException
{
    public LastAdminException()
        : base("Cannot deactivate the last Admin user.") { }
}