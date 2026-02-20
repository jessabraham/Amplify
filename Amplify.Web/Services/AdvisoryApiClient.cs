using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class AdvisoryApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public AdvisoryApiClient(HttpClient http, AuthStateProvider authState)
    {
        _http = http;
        _authState = authState;
    }

    private void AttachToken()
    {
        var token = _authState.Token;
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string?> ChatAsync(string message)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Advisory/chat",
            new { Message = message });

        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
        return result?.Response;
    }

    public async Task<string?> AnalyzeSignalAsync(SignalAnalysisDto dto)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Advisory/analyze-signal", dto);

        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<ChatResponse>();
        return result?.Response;
    }
}

public class ChatResponse
{
    public string Response { get; set; } = "";
}

public class SignalAnalysisDto
{
    public string Asset { get; set; } = "";
    public string SignalType { get; set; } = "";
    public string Regime { get; set; } = "";
    public decimal SetupScore { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal Target2 { get; set; }
    public decimal RiskPercent { get; set; }
}