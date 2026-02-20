using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Amplify.API.Hubs;

/// <summary>
/// SignalR hub for real-time pattern scanning updates.
/// Clients join their user-specific group to receive scan results.
/// </summary>
[Authorize]
public class PatternScanHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user-{userId}");
        await base.OnDisconnectedAsync(exception);
    }
}

/// <summary>
/// SignalR implementation of IScanNotificationService.
/// Registered in API's Program.cs so the background service can use it via DI.
/// </summary>
public class SignalRScanNotificationService : IScanNotificationService
{
    private readonly IHubContext<PatternScanHub> _hubContext;

    public SignalRScanNotificationService(IHubContext<PatternScanHub> hubContext)
        => _hubContext = hubContext;

    public async Task NotifyScanCompletedAsync(string userId, ScanNotification notification)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("ScanCompleted", notification);
    }

    public async Task NotifyPatternAlertAsync(string userId, ScanNotification notification)
    {
        await _hubContext.Clients
            .Group($"user-{userId}")
            .SendAsync("PatternAlert", notification);
    }
}