using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class SimulationApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public SimulationApiClient(HttpClient http, AuthStateProvider authState)
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

    /// <summary>
    /// Create a simulated trade linked to a signal.
    /// Called from Blazor after signal creation.
    /// </summary>
    public async Task<SimulatedTradeDto?> CreateSimulatedTradeAsync(CreateSimulatedTradeRequest request)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Simulation/create", request);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SimulatedTradeDto>();
    }

    /// <summary>
    /// Resolve all active trades (run simulation forward).
    /// </summary>
    public async Task<ResolveResultDto?> ResolveTradesAsync(string? asset = null)
    {
        AttachToken();
        var url = "api/Simulation/resolve";
        if (!string.IsNullOrEmpty(asset)) url += $"?asset={asset}";
        var response = await _http.PostAsync(url, null);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ResolveResultDto>();
    }

    /// <summary>
    /// Get active (open) simulated trades.
    /// </summary>
    public async Task<List<SimulatedTradeDto>> GetActiveTradesAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Simulation/active");
        if (!response.IsSuccessStatusCode) return new();
        return await response.Content.ReadFromJsonAsync<List<SimulatedTradeDto>>() ?? new();
    }

    /// <summary>
    /// Get resolved trade history.
    /// </summary>
    public async Task<List<SimulatedTradeDto>> GetHistoryAsync(int count = 50)
    {
        AttachToken();
        var response = await _http.GetAsync($"api/Simulation/history?count={count}");
        if (!response.IsSuccessStatusCode) return new();
        return await response.Content.ReadFromJsonAsync<List<SimulatedTradeDto>>() ?? new();
    }

    /// <summary>
    /// Get user's overall trading stats.
    /// </summary>
    public async Task<UserTradingStatsDto?> GetUserStatsAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Simulation/stats");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<UserTradingStatsDto>();
    }

    /// <summary>
    /// Delete a single simulated trade.
    /// </summary>
    public async Task<bool> DeleteTradeAsync(Guid tradeId)
    {
        AttachToken();
        var response = await _http.DeleteAsync($"api/Simulation/{tradeId}");
        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Clear all simulated trades for the current user.
    /// </summary>
    public async Task<int> ClearAllTradesAsync()
    {
        AttachToken();
        var response = await _http.DeleteAsync("api/Simulation/clear-all");
        if (!response.IsSuccessStatusCode) return 0;
        var result = await response.Content.ReadFromJsonAsync<ClearAllResultDto>();
        return result?.Deleted ?? 0;
    }
}

// ═══════════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════════

public class CreateSimulatedTradeRequest
{
    public Guid TradeSignalId { get; set; }
    // Pattern context
    public string? PatternType { get; set; }
    public string? PatternDirection { get; set; }
    public string? PatternTimeframe { get; set; }
    public decimal? PatternConfidence { get; set; }
    // Multi-timeframe context
    public string? TimeframeAlignment { get; set; }
    public string? RegimeAlignment { get; set; }
    public string? MAAlignment { get; set; }
    public string? VolumeProfile { get; set; }
    public decimal? RSIAtEntry { get; set; }
}

public class SimulatedTradeDto
{
    public Guid Id { get; set; }
    public string Asset { get; set; } = "";
    public string Direction { get; set; } = "";
    public string? Outcome { get; set; }
    public string Status { get; set; } = "";
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal? Target2 { get; set; }
    public decimal? ExitPrice { get; set; }
    public decimal? PnLPercent { get; set; }
    public decimal? PnLDollars { get; set; }
    public decimal? RMultiple { get; set; }
    public int DaysHeld { get; set; }
    public int MaxExpirationDays { get; set; }
    public decimal? HighestPriceSeen { get; set; }
    public decimal? LowestPriceSeen { get; set; }
    public decimal? MaxDrawdown { get; set; }
    public string? Pattern { get; set; }
    public string? PatternDirection2 { get; set; }
    public string? Timeframe { get; set; }
    public string? Regime { get; set; }
    public string? TimeframeAlignment { get; set; }
    public string? VolumeProfile { get; set; }
    public string? MAAlignment { get; set; }
    public decimal? AIConfidence { get; set; }
    public string? AIRecommendedAction { get; set; }
    public decimal? PatternConfidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Source { get; set; }  // "AI" or "Manual"
}

public class ResolveResultDto
{
    public int ResolvedCount { get; set; }
    public List<ResolvedTradeDto> Results { get; set; } = new();
}

public class ResolvedTradeDto
{
    public Guid Id { get; set; }
    public string Asset { get; set; } = "";
    public string Direction { get; set; } = "";
    public string Outcome { get; set; } = "";
    public decimal EntryPrice { get; set; }
    public decimal? ExitPrice { get; set; }
    public string? PnLPercent { get; set; }
    public string? PnLDollars { get; set; }
    public string? RMultiple { get; set; }
    public int DaysHeld { get; set; }
    public string? Pattern { get; set; }
    public string? Timeframe { get; set; }
}

public class UserTradingStatsDto
{
    public int TotalTrades { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public decimal WinRate { get; set; }
    public decimal AvgRMultiple { get; set; }
    public decimal TotalPnLPercent { get; set; }
    public decimal BestTrade { get; set; }
    public decimal WorstTrade { get; set; }
    public decimal AvgDaysHeld { get; set; }
    public decimal LongWinRate { get; set; }
    public decimal ShortWinRate { get; set; }
    public decimal AlignedWinRate { get; set; }
    public decimal ConflictingWinRate { get; set; }
}

public class ClearAllResultDto
{
    public int Deleted { get; set; }
}