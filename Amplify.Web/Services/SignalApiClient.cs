using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Domain.Enumerations;

namespace Amplify.Web.Services;

public class SignalApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public SignalApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<List<TradeSignalDto>> GetSignalsAsync(
        SignalSource? source = null, SignalStatus? status = null)
    {
        AttachToken();
        var url = "api/Signals";
        var queryParts = new List<string>();
        if (source.HasValue) queryParts.Add($"source={source.Value}");
        if (status.HasValue) queryParts.Add($"status={status.Value}");
        if (queryParts.Any()) url += "?" + string.Join("&", queryParts);

        return await _http.GetFromJsonAsync<List<TradeSignalDto>>(url) ?? new();
    }

    public async Task<TradeSignalDto?> CreateSignalAsync(TradeSignalDto dto)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Signals", dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TradeSignalDto>()
            : null;
    }

    public async Task<bool> AcceptSignalAsync(Guid id)
    {
        AttachToken();
        var response = await _http.PutAsync($"api/Signals/{id}/accept", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> RejectSignalAsync(Guid id)
    {
        AttachToken();
        var response = await _http.PutAsync($"api/Signals/{id}/reject", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ArchiveSignalAsync(Guid id)
    {
        AttachToken();
        var response = await _http.PutAsync($"api/Signals/{id}/archive", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteSignalAsync(Guid id)
    {
        AttachToken();
        var response = await _http.DeleteAsync($"api/Signals/{id}");
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> ClearAllSignalsAsync()
    {
        AttachToken();
        var response = await _http.DeleteAsync("api/Signals/clear-all");
        return response.IsSuccessStatusCode;
    }

    public async Task<SignalStatsDto?> GetStatsAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Signals/stats");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<SignalStatsDto>();
    }
}