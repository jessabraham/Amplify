using System.Net.Http.Headers;
using System.Net.Http.Json;
using Amplify.Application.Common.DTOs.Trading;

namespace Amplify.Web.Services;

public class PortfolioApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public PortfolioApiClient(HttpClient http, AuthStateProvider authState)
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

    // Summary
    public async Task<PortfolioSummaryDto?> GetSummaryAsync()
    {
        AttachToken();
        return await _http.GetFromJsonAsync<PortfolioSummaryDto>("api/Portfolio/summary");
    }

    // Open positions
    public async Task<List<PositionDto>> GetOpenPositionsAsync()
    {
        AttachToken();
        return await _http.GetFromJsonAsync<List<PositionDto>>("api/Portfolio/positions/open")
               ?? [];
    }

    // Closed positions
    public async Task<List<PositionDto>> GetClosedPositionsAsync()
    {
        AttachToken();
        return await _http.GetFromJsonAsync<List<PositionDto>>("api/Portfolio/positions/closed")
               ?? [];
    }

    // Open a new position
    public async Task<PositionDto?> OpenPositionAsync(PositionDto dto)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Portfolio/positions", dto);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadFromJsonAsync<PositionDto>();

        var error = await response.Content.ReadAsStringAsync();
        throw new Exception(error);
    }

    // Close a position
    public async Task<PositionDto?> ClosePositionAsync(ClosePositionDto dto)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Portfolio/positions/close", dto);
        return response.IsSuccessStatusCode
            ? await response.Content.ReadFromJsonAsync<PositionDto>()
            : null;
    }

    // Delete a position
    public async Task<bool> DeletePositionAsync(Guid id)
    {
        AttachToken();
        var response = await _http.DeleteAsync($"api/Portfolio/positions/{id}");
        return response.IsSuccessStatusCode;
    }

    // Snapshots
    public async Task<List<PortfolioSnapshotDto>> GetSnapshotsAsync(int days = 30)
    {
        AttachToken();
        return await _http.GetFromJsonAsync<List<PortfolioSnapshotDto>>(
            $"api/Portfolio/snapshots?days={days}") ?? [];
    }

    public async Task<bool> TakeSnapshotAsync()
    {
        AttachToken();
        var response = await _http.PostAsync("api/Portfolio/snapshots", null);
        return response.IsSuccessStatusCode;
    }
}