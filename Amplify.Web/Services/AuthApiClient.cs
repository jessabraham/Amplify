using System.Net.Http.Json;
using Amplify.Application.Common.DTOs.Admin;

namespace Amplify.Web.Services;

public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var response = await _http.PostAsJsonAsync("api/Auth/login", dto);
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<AuthResponseDto>();
    }
}