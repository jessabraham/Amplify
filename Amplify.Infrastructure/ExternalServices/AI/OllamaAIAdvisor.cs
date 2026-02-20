using Amplify.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using OllamaSharp;
using System.Runtime.CompilerServices;
using System.Text;

namespace Amplify.Infrastructure.ExternalServices.AI;

public class OllamaAIAdvisor : IAIAdvisor
{
    private readonly Uri _baseUrl;
    private readonly string _model;

    public OllamaAIAdvisor(IConfiguration config)
    {
        var url = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _baseUrl = new Uri(url);
        _model = config["Ollama:Model"] ?? "qwen3:8b";
    }

    public async Task<string> GetAdvisoryAsync(string prompt)
    {
        var client = new OllamaApiClient(_baseUrl)
        {
            SelectedModel = _model
        };

        var sb = new StringBuilder();

        await foreach (var chunk in client.GenerateAsync(prompt))
        {
            sb.Append(chunk?.Response ?? "");
        }

        return sb.Length > 0 ? sb.ToString() : "No response from AI.";
    }

    public async IAsyncEnumerable<string> StreamAdvisoryAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var client = new OllamaApiClient(_baseUrl)
        {
            SelectedModel = _model
        };

        await foreach (var chunk in client.GenerateAsync(prompt).WithCancellation(cancellationToken))
        {
            if (chunk?.Response is not null)
                yield return chunk.Response;
        }
    }
}