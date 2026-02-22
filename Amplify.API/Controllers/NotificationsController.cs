using Amplify.Domain.Entities.Trading;
using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Amplify.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

    public NotificationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get unread notification count.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _context.Notifications
            .CountAsync(n => n.UserId == UserId && !n.IsRead);
        return Ok(new { count });
    }

    /// <summary>
    /// Get recent notifications (unread first, then recent read).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int count = 20)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == UserId)
            .OrderByDescending(n => n.IsRead ? 0 : 1)  // unread first
            .ThenByDescending(n => n.CreatedAt)
            .Take(Math.Min(count, 50))
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                Type = n.Type.ToString(),
                Priority = n.Priority.ToString(),
                n.LinkUrl,
                n.IsRead,
                n.CreatedAt
            })
            .ToListAsync();

        return Ok(notifications);
    }

    /// <summary>
    /// Mark a notification as read.
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == UserId);
        if (notification is null) return NotFound();

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Mark all notifications as read.
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var unread = await _context.Notifications
            .Where(n => n.UserId == UserId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { marked = unread.Count });
    }

    /// <summary>
    /// Delete old read notifications (cleanup).
    /// </summary>
    [HttpDelete("cleanup")]
    public async Task<IActionResult> Cleanup([FromQuery] int olderThanDays = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-olderThanDays);
        var old = await _context.Notifications
            .Where(n => n.UserId == UserId && n.IsRead && n.CreatedAt < cutoff)
            .ToListAsync();

        _context.Notifications.RemoveRange(old);
        await _context.SaveChangesAsync();
        return Ok(new { deleted = old.Count });
    }
}