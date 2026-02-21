namespace Amplify.Domain.Enumerations;

public enum SignalStatus
{
    Pending,    // Awaiting user review (AI signals start here)
    Accepted,   // User accepted — simulated trade created
    Rejected,   // User dismissed
    Archived    // Manually archived
}