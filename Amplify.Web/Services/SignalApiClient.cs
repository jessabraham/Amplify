using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amplify.Application.Common.DTOs.Trading;

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

    public async Task<List<TradeSignalDto>> GetSignalsAsync()
    {
        AttachToken();
        return await _http.GetFromJsonAsync<List<TradeSignalDto>>("api/Signals")
               ?? new();
    }

    public async Task<TradeSignalDto?> CreateSignalAsync(TradeSignalDto dto)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Signals", dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<TradeSignalDto>()
            : null;
    }
}