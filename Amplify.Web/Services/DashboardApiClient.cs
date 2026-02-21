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
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new Exception($"API returned {(int)response.StatusCode}: {body}");
        }
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
    // New
    public decimal TotalInvested { get; set; }
    public decimal TotalUnrealizedPnL { get; set; }
    public int OpenPositionCount { get; set; }
    public decimal StartingCapital { get; set; }
    public decimal CashAvailable { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal PortfolioValue { get; set; }
    public List<DashPositionItem> TopPositions { get; set; } = new();
    public List<DashPatternItem> RecentPatterns { get; set; } = new();
    public List<DashRegimeItem> RecentRegimes { get; set; } = new();
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
public class DashPositionItem
{
    public string Symbol { get; set; } = "";
    public string SignalType { get; set; } = "";
    public decimal Quantity { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal? ReturnPercent { get; set; }
    public decimal StopLoss { get; set; }
    public decimal? Target1 { get; set; }
    public DateTime EntryDateUtc { get; set; }
}

public class DashPatternItem
{
    public string Asset { get; set; } = "";
    public string PatternType { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal Confidence { get; set; }
    public string? AIAnalysis { get; set; }
    public decimal DetectedAtPrice { get; set; }
    public decimal SuggestedEntry { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DashRegimeItem
{
    public string Symbol { get; set; } = "";
    public string Regime { get; set; } = "";
    public decimal Confidence { get; set; }
    public DateTime DetectedAt { get; set; }
}