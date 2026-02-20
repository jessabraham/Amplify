using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amplify.Application.Common.DTOs.Market;
using Amplify.Application.Common.Models;

namespace Amplify.Web.Services;

public class RegimeApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public RegimeApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<RegimeResultDto?> DetectRegimeAsync(string symbol, List<Candle> candles)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync($"api/Regime/detect?symbol={symbol}", candles);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RegimeResultDto>()
            : null;
    }

    public async Task<RegimeResultDto?> GetLatestRegimeAsync(string symbol)
    {
        AttachToken();
        try
        {
            return await _http.GetFromJsonAsync<RegimeResultDto>($"api/Regime/{symbol}/latest");
        }
        catch { return null; }
    }

    public async Task<List<RegimeHistoryDto>> GetHistoryAsync(string symbol, int days = 30)
    {
        AttachToken();
        return await _http.GetFromJsonAsync<List<RegimeHistoryDto>>(
            $"api/Regime/{symbol}/history?days={days}") ?? [];
    }
}