using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class BacktestApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public BacktestApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<List<BacktestDto>> GetBacktestsAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Backtest");
        if (!response.IsSuccessStatusCode) return new();
        return await response.Content.ReadFromJsonAsync<List<BacktestDto>>() ?? new();
    }

    public async Task<BacktestDto?> RunBacktestAsync(RunBacktestDto dto)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Backtest/run", dto);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<BacktestDto>();
    }
}

public class BacktestDto
{
    public Guid Id { get; set; }
    public string Asset { get; set; } = "";
    public string AssetClass { get; set; } = "";
    public string SignalType { get; set; } = "";
    public string Regime { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
    public int TotalTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal MaxDrawdown { get; set; }
    public decimal NetPnL { get; set; }
    public decimal SharpeRatio { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class RunBacktestDto
{
    public string Asset { get; set; } = "";
    public int AssetClass { get; set; }
    public int SignalType { get; set; }
    public int Regime { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal InitialCapital { get; set; }
}