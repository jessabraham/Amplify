namespace Amplify.Application.Common.Interfaces.AI;

public interface IAIAdvisor
{
    Task<string> GetAdvisoryAsync(string prompt);
    IAsyncEnumerable<string> StreamAdvisoryAsync(string prompt, CancellationToken cancellationToken = default);
}