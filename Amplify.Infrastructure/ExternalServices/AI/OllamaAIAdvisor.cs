using Amplify.Application.Common.Interfaces.AI;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Amplify.Infrastructure.ExternalServices.AI;

public class OllamaAIAdvisor : IAIAdvisor
{
    private readonly string _baseUrl;
    private readonly string _model;
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(300)
    };

    public OllamaAIAdvisor(IConfiguration config)
    {
        _baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model = config["Ollama:Model"] ?? "qwen3:8b";
    }

    public async Task<string> GetAdvisoryAsync(string prompt)
    {
        var request = new
        {
            model = _model,
            prompt = prompt,
            stream = false,
            options = new { num_predict = 2048 },
            think = false  // Disable Qwen3 thinking — much faster for structured output
        };

        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/generate", request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("response", out var resp))
            return resp.GetString() ?? "No response from AI.";

        return "No response from AI.";
    }

    public async IAsyncEnumerable<string> StreamAdvisoryAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = _model,
            prompt = prompt,
            stream = true,
            options = new { num_predict = 2048 },
            think = false
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/generate")
        {
            Content = JsonContent.Create(request)
        };

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
        {
            if (string.IsNullOrEmpty(line)) continue;

            string? text = null;
            bool done = false;

            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("response", out var resp))
                    text = resp.GetString();
                if (doc.RootElement.TryGetProperty("done", out var d))
                    done = d.GetBoolean();
            }
            catch { }

            if (!string.IsNullOrEmpty(text))
                yield return text;

            if (done || cancellationToken.IsCancellationRequested)
                break;
        }
    }
}