using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class AnalyticsApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public AnalyticsApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<AnalyticsData?> GetAnalyticsAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Analytics");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AnalyticsData>();
    }
}

public class AnalyticsData
{
    // Signal stats
    public int TotalSignals { get; set; }
    public int ActiveSignals { get; set; }
    public double AvgScore { get; set; }
    public int HighConviction { get; set; }
    public int LowConviction { get; set; }
    public List<TypeCount> SignalTypeDistribution { get; set; } = new();
    public List<AssetCount> AssetDistribution { get; set; } = new();
    public List<RegimeCount> RegimeDistribution { get; set; } = new();
    public List<ScoreBucket> ScoreDistribution { get; set; } = new();
    public List<DateCount> SignalsByDate { get; set; } = new();

    // Risk stats
    public double AvgRisk { get; set; }
    public decimal MaxRisk { get; set; }
    public decimal MinRisk { get; set; }

    // Override stats
    public int TotalOverrides { get; set; }
    public int Accepted { get; set; }
    public int Rejected { get; set; }
    public int Modified { get; set; }
    public List<ReasonCount> ReasonBreakdown { get; set; } = new();

    // Outcome stats
    public int CorrectDecisions { get; set; }
    public int IncorrectDecisions { get; set; }
    public double WinRate { get; set; }
    public decimal TotalPnL { get; set; }
}

public class TypeCount { public string Type { get; set; } = ""; public int Count { get; set; } }
public class AssetCount { public string Asset { get; set; } = ""; public int Count { get; set; } }
public class RegimeCount { public string Regime { get; set; } = ""; public int Count { get; set; } }
public class ScoreBucket { public string Bucket { get; set; } = ""; public int Count { get; set; } }
public class DateCount { public string Date { get; set; } = ""; public int Count { get; set; } }
public class ReasonCount { public string Reason { get; set; } = ""; public int Count { get; set; } }