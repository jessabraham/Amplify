using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amplify.Web.Services;

public class PortfolioAdvisorApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public PortfolioAdvisorApiClient(HttpClient http, AuthStateProvider authState)
    {
        _http = http;
        _http.Timeout = TimeSpan.FromSeconds(180);
        _authState = authState;
    }

    private void AttachToken()
    {
        var token = _authState.Token;
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<PortfolioAllocationResult?> GetAllocationAdviceAsync()
    {
        AttachToken();
        var response = await _http.PostAsync("api/PortfolioAdvisor/allocate", null);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();

        // Try to parse structured result
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<PortfolioAllocationResult>(json, options);
        }
        catch
        {
            // If AI returned raw text, wrap it
            return new PortfolioAllocationResult { Summary = json };
        }
    }

    public async Task<List<AdviceHistoryItem>> GetAdviceHistoryAsync(int count = 10)
    {
        AttachToken();
        var response = await _http.GetAsync($"api/PortfolioAdvisor/history?count={count}");
        if (!response.IsSuccessStatusCode) return new();

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return await response.Content.ReadFromJsonAsync<List<AdviceHistoryItem>>(options) ?? new();
    }

    public async Task<AdvisorScorecard?> GetScorecardAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/PortfolioAdvisor/scorecard");
        if (!response.IsSuccessStatusCode) return null;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return await response.Content.ReadFromJsonAsync<AdvisorScorecard>(options);
    }
}

public class PortfolioAllocationResult
{
    public string Summary { get; set; } = "";
    public decimal TotalSuggestedAllocation { get; set; }
    public decimal CashRetained { get; set; }
    public List<AllocationSuggestion> Allocations { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public string DiversificationScore { get; set; } = "";
}

public class AllocationSuggestion
{
    public string Symbol { get; set; } = "";
    public decimal SuggestedBudget { get; set; }
    public string Direction { get; set; } = "Long";
    public string Confidence { get; set; } = "Medium";
    public string Rationale { get; set; } = "";
    public string RiskNote { get; set; } = "";
    public decimal PortfolioPercent { get; set; }
    public bool Skip { get; set; }
    public string? SkipReason { get; set; }
    public List<string> PatternIds { get; set; } = new();
    public string? PatternSummary { get; set; }
}

public class AdviceHistoryItem
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Summary { get; set; } = "";
    public string DiversificationScore { get; set; } = "";
    public decimal TotalSuggestedAllocation { get; set; }
    public decimal CashRetained { get; set; }
    public decimal CashAvailable { get; set; }
    public decimal TotalInvested { get; set; }
    public int OpenPositionCount { get; set; }
    public int WatchlistCount { get; set; }
    public int TotalAllocations { get; set; }
    public int AllocationsFollowed { get; set; }
    public string ResponseJson { get; set; } = "";
}

public class AdvisorScorecard
{
    public int AdviceCount { get; set; }
    public int TotalAllocations { get; set; }
    public int TotalExecuted { get; set; }
    public int TotalProfitable { get; set; }
    public decimal ExecutionRate { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalPnL { get; set; }
    public int AiExecuted { get; set; }
    public int ManualExecuted { get; set; }
    public List<ConfidenceBreakdown> ByConfidence { get; set; } = new();
}

public class ConfidenceBreakdown
{
    public string Confidence { get; set; } = "";
    public int Total { get; set; }
    public int Executed { get; set; }
    public int Profitable { get; set; }
    public decimal WinRate { get; set; }
    public decimal TotalPnL { get; set; }
}