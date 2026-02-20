namespace Amplify.Domain.Interfaces.Common;

public interface IAuditableEntity
{
    string CreatedByUserId { get; set; }
    string? UpdatedByUserId { get; set; }
}