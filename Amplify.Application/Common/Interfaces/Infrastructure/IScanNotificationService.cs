using Amplify.Application.Common.DTOs.Trading;

namespace Amplify.Application.Common.Interfaces.Infrastructure;

/// <summary>
/// Sends real-time scan notifications to connected clients.
/// Implemented in API project using SignalR. Referenced by Infrastructure's background scanner.
/// This keeps Infrastructure free of any SignalR or API dependency.
/// </summary>
public interface IScanNotificationService
{
    Task NotifyScanCompletedAsync(string userId, ScanNotification notification);
    Task NotifyPatternAlertAsync(string userId, ScanNotification notification);
}