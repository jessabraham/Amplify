using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Amplify.Web.Services;

public class NotificationApiClient
{
    private readonly HttpClient _http;
    private readonly AuthStateProvider _authState;

    public NotificationApiClient(HttpClient http, AuthStateProvider authState)
    {
        _http = http;
        _authState = authState;
    }

    private void AttachToken()
    {
        var token = _authState.Token;
        if (!string.IsNullOrEmpty(token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<int> GetUnreadCountAsync()
    {
        AttachToken();
        try
        {
            var result = await _http.GetFromJsonAsync<JsonElement>("api/Notifications/unread-count");
            return result.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
        }
        catch { return 0; }
    }

    public async Task<List<NotificationItem>> GetNotificationsAsync(int count = 20)
    {
        AttachToken();
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return await _http.GetFromJsonAsync<List<NotificationItem>>($"api/Notifications?count={count}", options) ?? new();
        }
        catch { return new(); }
    }

    public async Task MarkReadAsync(Guid id)
    {
        AttachToken();
        try { await _http.PostAsync($"api/Notifications/{id}/read", null); }
        catch { }
    }

    public async Task MarkAllReadAsync()
    {
        AttachToken();
        try { await _http.PostAsync("api/Notifications/read-all", null); }
        catch { }
    }
}

public class NotificationItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "";
    public string Priority { get; set; } = "";
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}