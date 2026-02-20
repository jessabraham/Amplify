using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class OverrideApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public OverrideApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<List<OverrideDto>> GetOverridesAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Overrides");
        if (!response.IsSuccessStatusCode) return new();
        return await response.Content.ReadFromJsonAsync<List<OverrideDto>>() ?? new();
    }

    public async Task<bool> CreateOverrideAsync(CreateOverrideDto dto)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Overrides", dto);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateOutcomeAsync(Guid id, decimal pnl, bool wasCorrect)
    {
        AttachToken();
        var response = await _http.PutAsJsonAsync($"api/Overrides/{id}/outcome",
            new { ActualPnL = pnl, WasCorrect = wasCorrect });
        return response.IsSuccessStatusCode;
    }
}

public class OverrideDto
{
    public Guid Id { get; set; }
    public Guid TradeSignalId { get; set; }
    public string Asset { get; set; } = "";
    public string SignalType { get; set; } = "";
    public string OverrideType { get; set; } = "";
    public string Reason { get; set; } = "";
    public string? Notes { get; set; }
    public decimal? ModifiedEntryPrice { get; set; }
    public decimal? ModifiedStopLoss { get; set; }
    public decimal? ModifiedTarget1 { get; set; }
    public decimal? ModifiedTarget2 { get; set; }
    public decimal? ActualPnL { get; set; }
    public bool? WasCorrect { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateOverrideDto
{
    public Guid TradeSignalId { get; set; }
    public int OverrideType { get; set; }
    public int Reason { get; set; }
    public string? Notes { get; set; }
    public decimal? ModifiedEntryPrice { get; set; }
    public decimal? ModifiedStopLoss { get; set; }
    public decimal? ModifiedTarget1 { get; set; }
    public decimal? ModifiedTarget2 { get; set; }
}