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

// ═══════════════════════════════════════════════════════════════
// SCAN RESULT
// ═══════════════════════════════════════════════════════════════

public class ScanResultData
{
    public string Symbol { get; set; } = "";
    public int TotalPatterns { get; set; }
    public decimal CurrentPrice { get; set; }
    public string DataSource { get; set; } = ""; // "Alpaca" or "Sample Data"

    // Multi-timeframe
    public string CombinedRegime { get; set; } = "";
    public decimal CombinedRegimeConfidence { get; set; }
    public string RegimeAlignment { get; set; } = "";
    public string DirectionAlignment { get; set; } = "";
    public decimal AlignmentScore { get; set; }
    public List<TimeframeSummaryData> TimeframeSummaries { get; set; } = new();

    // Context layers
    public ContextData? Context { get; set; }
    public List<ContextData> TimeframeContexts { get; set; } = new();

    // Patterns + AI
    public List<ScannedPatternDto> Patterns { get; set; } = new();
    public AIAnalysisData? AIAnalysis { get; set; }

    // Chart data — all three timeframes
    public List<ChartCandleDto> ChartCandles { get; set; } = new();
    public List<ChartCandleDto> ChartCandles1H { get; set; } = new();
    public List<ChartCandleDto> ChartCandles4H { get; set; } = new();
    public List<ChartCandleDto> ChartCandlesWeekly { get; set; } = new();
}

public class ChartCandleDto
{
    public long Time { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// TIMEFRAME
// ═══════════════════════════════════════════════════════════════

public class TimeframeSummaryData
{
    public string Timeframe { get; set; } = "";
    public int PatternCount { get; set; }
    public string DominantDirection { get; set; } = "";
    public int BullishCount { get; set; }
    public int BearishCount { get; set; }
    public string Regime { get; set; } = "";
    public decimal RegimeConfidence { get; set; }
    public decimal? RSI { get; set; }
    public string VolumeProfile { get; set; } = "";
}

// ═══════════════════════════════════════════════════════════════
// CONTEXT LAYERS
// ═══════════════════════════════════════════════════════════════

public class ContextData
{
    public string Timeframe { get; set; } = "Daily";
    public decimal VolumeRatio { get; set; }
    public string VolumeProfile { get; set; } = "";
    public decimal? NearestSupport { get; set; }
    public decimal? NearestResistance { get; set; }
    public decimal? DistToSupportPct { get; set; }
    public decimal? DistToResistancePct { get; set; }
    public decimal? SMA20 { get; set; }
    public decimal? SMA50 { get; set; }
    public decimal? SMA200 { get; set; }
    public decimal? DistFromSMA200Pct { get; set; }
    public string MAAlignment { get; set; } = "";
    public decimal? RSI { get; set; }
    public string RSIZone { get; set; } = "";
    public decimal? ATRPercent { get; set; }
    public int ConsecutiveUpDays { get; set; }
    public int ConsecutiveDownDays { get; set; }
    public List<KeyLevelData> KeyLevels { get; set; } = new();
}

public class KeyLevelData
{
    public decimal Price { get; set; }
    public string Type { get; set; } = "";
    public int TouchCount { get; set; }
}

// ═══════════════════════════════════════════════════════════════
// PATTERN + AI
// ═══════════════════════════════════════════════════════════════

public class ScannedPatternDto
{
    public string PatternName { get; set; } = "";
    public string PatternType { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Timeframe { get; set; } = "";
    public decimal Confidence { get; set; }
    public decimal HistoricalWinRate { get; set; }
    public string Description { get; set; } = "";
    public decimal SuggestedEntry { get; set; }
    public decimal SuggestedStop { get; set; }
    public decimal SuggestedTarget { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int StartCandleIndex { get; set; }
    public int EndCandleIndex { get; set; }
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

// ═══════════════════════════════════════════════════════════════
// HISTORY
// ═══════════════════════════════════════════════════════════════

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