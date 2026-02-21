using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class SettingsApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public SettingsApiClient(HttpClient http, AuthStateProvider authState)
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

    // Profile
    public async Task<ProfileData?> GetProfileAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Settings/profile");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProfileData>();
    }

    public async Task<string?> UpdateProfileAsync(string displayName, string email)
    {
        AttachToken();
        var response = await _http.PutAsJsonAsync("api/Settings/profile",
            new { DisplayName = displayName, Email = email });
        if (response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsStringAsync();
    }

    public async Task<string?> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        AttachToken();
        var response = await _http.PutAsJsonAsync("api/Settings/password",
            new { CurrentPassword = currentPassword, NewPassword = newPassword });
        if (response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsStringAsync();
    }

    // AI Config
    public async Task<AIConfigData?> GetAIConfigAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Settings/ai");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AIConfigData>();
    }

    // Risk Config
    public async Task<RiskConfigData?> GetRiskConfigAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Settings/risk");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<RiskConfigData>();
    }

    // Portfolio Balance
    public async Task<PortfolioBalanceData?> GetPortfolioBalanceAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Settings/portfolio-balance");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<PortfolioBalanceData>();
    }

    public async Task<bool> UpdateStartingCapitalAsync(decimal amount)
    {
        AttachToken();
        var response = await _http.PutAsJsonAsync("api/Settings/portfolio-balance",
            new { StartingCapital = amount });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UpdateAiBudgetAsync(decimal percent)
    {
        AttachToken();
        var response = await _http.PutAsJsonAsync("api/Settings/ai-budget",
            new { AiTradingBudgetPercent = percent });
        return response.IsSuccessStatusCode;
    }
}

public class ProfileData
{
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginUtc { get; set; }
}

public class AIConfigData
{
    public string BaseUrl { get; set; } = "";
    public string Model { get; set; } = "";
    public double Temperature { get; set; }
    public int MaxTokens { get; set; }
}

public class RiskConfigData
{
    public double DefaultRiskPercent { get; set; }
    public double MaxRiskPercent { get; set; }
    public int MaxPositionSize { get; set; }
    public int DefaultPortfolioSize { get; set; }
}

public class PortfolioBalanceData
{
    public decimal StartingCapital { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CashAvailable { get; set; }
    public int OpenPositionCount { get; set; }
    public decimal AiTradingBudgetPercent { get; set; }
    public decimal AiTradingBudgetDollars { get; set; }
    public decimal AiCashUsed { get; set; }
    public decimal AiCashRemaining { get; set; }
}