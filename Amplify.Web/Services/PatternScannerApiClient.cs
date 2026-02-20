using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class PatternScannerApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public PatternScannerApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<ScanResultData?> ScanSymbolAsync(string symbol, decimal minConfidence, string direction, string timeframe, bool enableAI)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/PatternScanner/scan", new
        {
            Symbol = symbol,
            MinConfidence = minConfidence,
            Direction = direction,
            Timeframe = timeframe,
            EnableAI = enableAI
        });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ScanResultData>();
    }

    public async Task<List<PatternHistoryDto>> GetHistoryAsync(string? symbol = null)
    {
        AttachToken();
        var url = "api/PatternScanner/history";
        if (!string.IsNullOrEmpty(symbol)) url += $"?symbol={symbol}";
        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode) return new();
        return await response.Content.ReadFromJsonAsync<List<PatternHistoryDto>>() ?? new();
    }
}

public class ScanResultData
{
    public string Symbol { get; set; } = "";
    public int TotalPatterns { get; set; }
    public decimal CurrentPrice { get; set; }
    public List<ScannedPatternDto> Patterns { get; set; } = new();
    public AIAnalysisData? AIAnalysis { get; set; }
}

public class ScannedPatternDto
{
    public string PatternName { get; set; } = "";
    public string PatternType { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal Confidence { get; set; }
    public decimal HistoricalWinRate { get; set; }
    public string Description { get; set; } = "";
    public decimal SuggestedEntry { get; set; }
    public decimal SuggestedStop { get; set; }
    public decimal SuggestedTarget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    // AI fields
    public string AIGrade { get; set; } = "";
    public bool AIValid { get; set; } = true;
    public string AIReason { get; set; } = "";
}

public class AIAnalysisData
{
    public string OverallBias { get; set; } = "";
    public decimal OverallConfidence { get; set; }
    public string Summary { get; set; } = "";
    public string DetailedAnalysis { get; set; } = "";
    public string RecommendedAction { get; set; } = "";
    public decimal? RecommendedEntry { get; set; }
    public decimal? RecommendedStop { get; set; }
    public decimal? RecommendedTarget { get; set; }
    public string RiskReward { get; set; } = "";
}

public class PatternHistoryDto
{
    public Guid Id { get; set; }
    public string Asset { get; set; } = "";
    public string PatternType { get; set; } = "";
    public string Direction { get; set; } = "";
    public decimal Confidence { get; set; }
    public decimal HistoricalWinRate { get; set; }
    public string Description { get; set; } = "";
    public decimal DetectedAtPrice { get; set; }
    public decimal SuggestedEntry { get; set; }
    public decimal SuggestedStop { get; set; }
    public decimal SuggestedTarget { get; set; }
    public bool? WasCorrect { get; set; }
    public decimal? ActualPnLPercent { get; set; }
    public DateTime CreatedAt { get; set; }
}