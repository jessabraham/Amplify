using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amplify.Application.Common.DTOs.Trading;

namespace Amplify.Web.Services;

public class RiskApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public RiskApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<RiskAssessmentDto?> CalculateAsync(RiskInputDto input)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Risk/calculate", input);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<RiskAssessmentDto>()
            : null;
    }
}