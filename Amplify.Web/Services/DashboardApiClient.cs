using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class DashboardApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public DashboardApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<DashboardData?> GetDashboardAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Dashboard");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<DashboardData>();
    }
}

public class DashboardData
{
    public int TotalSignals { get; set; }
    public int LongCount { get; set; }
    public int ShortCount { get; set; }
    public decimal AvgScore { get; set; }
    public decimal AvgRisk { get; set; }
    public int HighConviction { get; set; }
    public List<RegimeItem> RegimeBreakdown { get; set; } = new();
    public List<AssetItem> AssetBreakdown { get; set; } = new();
    public List<RecentSignal> RecentSignals { get; set; } = new();
}

public class RegimeItem
{
    public string Regime { get; set; } = "";
    public int Count { get; set; }
}

public class AssetItem
{
    public string Asset { get; set; } = "";
    public int Count { get; set; }
}

public class RecentSignal
{
    public string Asset { get; set; } = "";
    public string SignalType { get; set; } = "";
    public decimal SetupScore { get; set; }
    public decimal EntryPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}