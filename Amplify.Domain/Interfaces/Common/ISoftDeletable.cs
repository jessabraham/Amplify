namespace Amplify.Domain.Interfaces.Common;

public interface ISoftDeletable
{
    bool IsActive { get; set; }
    DateTime? DeactivatedAt { get; set; }
}