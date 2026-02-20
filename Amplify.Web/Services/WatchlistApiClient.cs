using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class WatchlistApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public WatchlistApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<List<WatchlistItemData>> GetWatchlistAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Watchlist");
        if (!response.IsSuccessStatusCode) return new();
        return await response.Content.ReadFromJsonAsync<List<WatchlistItemData>>() ?? new();
    }

    public async Task<WatchlistItemData?> AddSymbolAsync(string symbol, bool enableAI = true, decimal minConfidence = 60, int intervalMinutes = 30)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Watchlist", new
        {
            Symbol = symbol,
            EnableAI = enableAI,
            MinConfidence = minConfidence,
            ScanIntervalMinutes = intervalMinutes
        });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<WatchlistItemData>();
    }

    public async Task<bool> UpdateItemAsync(Guid id, bool isActive, bool enableAI, decimal minConfidence, int intervalMinutes)
    {
        AttachToken();
        var response = await _http.PutAsJsonAsync($"api/Watchlist/{id}", new
        {
            IsActive = isActive,
            EnableAI = enableAI,
            MinConfidence = minConfidence,
            ScanIntervalMinutes = intervalMinutes
        });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveItemAsync(Guid id)
    {
        AttachToken();
        var response = await _http.DeleteAsync($"api/Watchlist/{id}");
        return response.IsSuccessStatusCode;
    }
}

public class WatchlistItemData
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = "";
    public bool IsActive { get; set; }
    public bool EnableAI { get; set; }
    public decimal MinConfidence { get; set; }
    public int ScanIntervalMinutes { get; set; }
    public DateTime? LastScannedAt { get; set; }
    public int LastPatternCount { get; set; }
    public string? LastBias { get; set; }
}

/// <summary>
/// Matches the ScanNotification from the SignalR hub.
/// </summary>
public class ScanNotificationData
{
    public string Symbol { get; set; } = "";
    public int PatternCount { get; set; }
    public decimal CurrentPrice { get; set; }
    public string? OverallBias { get; set; }
    public decimal? AIConfidence { get; set; }
    public string? RecommendedAction { get; set; }
    public string? TopPattern { get; set; }
    public decimal? TopPatternConfidence { get; set; }
    public DateTime ScannedAt { get; set; }
    public bool IsAlert { get; set; }
    public string? AlertMessage { get; set; }
}