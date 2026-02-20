using System.Net.Http.Headers;

namespace Amplify.Web.Services;

public class UserAdminApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public UserAdminApiClient(HttpClient http, AuthStateProvider authState)
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

    public async Task<List<UserDto>> GetUsersAsync()
    {
        AttachToken();
        var response = await _http.GetAsync("api/Users");
        if (!response.IsSuccessStatusCode) return new();
        return await response.Content.ReadFromJsonAsync<List<UserDto>>() ?? new();
    }

    public async Task<InviteResultDto?> InviteUserAsync(string email, string? displayName, string role)
    {
        AttachToken();
        var response = await _http.PostAsJsonAsync("api/Users/invite",
            new { Email = email, DisplayName = displayName, Role = role });
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<InviteResultDto>();
    }

    public async Task<bool> ChangeRoleAsync(string userId, string role)
    {
        AttachToken();
        var response = await _http.PutAsJsonAsync($"api/Users/{userId}/role",
            new { Role = role });
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> LockUserAsync(string userId)
    {
        AttachToken();
        var response = await _http.PutAsync($"api/Users/{userId}/lock", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> UnlockUserAsync(string userId)
    {
        AttachToken();
        var response = await _http.PutAsync($"api/Users/{userId}/unlock", null);
        return response.IsSuccessStatusCode;
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        AttachToken();
        var response = await _http.DeleteAsync($"api/Users/{userId}");
        return response.IsSuccessStatusCode;
    }
}

public class UserDto
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginUtc { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLocked { get; set; }
}

public class InviteResultDto
{
    public string Id { get; set; } = "";
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Role { get; set; } = "";
    public string TempPassword { get; set; } = "";
}