using Amplify.Domain.Entities.Trading;
using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Amplify.API.Controllers.Trading;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WatchlistController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public WatchlistController(ApplicationDbContext context) => _context = context;

    /// <summary>
    /// Get all watchlist items for the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetWatchlist()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var items = await _context.Set<WatchlistItem>()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.IsActive)
            .ThenBy(w => w.Symbol)
            .Select(w => new WatchlistItemDto
            {
                Id = w.Id,
                Symbol = w.Symbol,
                IsActive = w.IsActive,
                EnableAI = w.EnableAI,
                MinConfidence = w.MinConfidence,
                ScanIntervalMinutes = w.ScanIntervalMinutes,
                LastScannedAt = w.LastScannedAt,
                LastPatternCount = w.LastPatternCount,
                LastBias = w.LastBias
            })
            .ToListAsync();

        return Ok(items);
    }

    /// <summary>
    /// Add a symbol to the watchlist.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> AddToWatchlist([FromBody] AddWatchlistRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var symbol = request.Symbol.Trim().ToUpper();

        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required.");

        // Check duplicate
        var exists = await _context.Set<WatchlistItem>()
            .AnyAsync(w => w.UserId == userId && w.Symbol == symbol);

        if (exists)
            return BadRequest($"{symbol} is already on your watchlist.");

        // Limit to 20 items
        var count = await _context.Set<WatchlistItem>()
            .CountAsync(w => w.UserId == userId);

        if (count >= 20)
            return BadRequest("Watchlist is limited to 20 symbols.");

        var item = new WatchlistItem
        {
            Symbol = symbol,
            EnableAI = request.EnableAI,
            MinConfidence = request.MinConfidence,
            ScanIntervalMinutes = Math.Clamp(request.ScanIntervalMinutes, 5, 1440),
            UserId = userId
        };

        _context.Set<WatchlistItem>().Add(item);
        await _context.SaveChangesAsync();

        return Ok(new WatchlistItemDto
        {
            Id = item.Id,
            Symbol = item.Symbol,
            IsActive = item.IsActive,
            EnableAI = item.EnableAI,
            MinConfidence = item.MinConfidence,
            ScanIntervalMinutes = item.ScanIntervalMinutes
        });
    }

    /// <summary>
    /// Update watchlist item settings.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWatchlistItem(Guid id, [FromBody] UpdateWatchlistRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var item = await _context.Set<WatchlistItem>()
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (item is null) return NotFound();

        item.IsActive = request.IsActive;
        item.EnableAI = request.EnableAI;
        item.MinConfidence = request.MinConfidence;
        item.ScanIntervalMinutes = Math.Clamp(request.ScanIntervalMinutes, 5, 1440);
        item.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>
    /// Remove a symbol from the watchlist.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveFromWatchlist(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var item = await _context.Set<WatchlistItem>()
            .FirstOrDefaultAsync(w => w.Id == id && w.UserId == userId);

        if (item is null) return NotFound();

        _context.Set<WatchlistItem>().Remove(item);
        await _context.SaveChangesAsync();
        return Ok();
    }
}

// ===== DTOs =====

public class WatchlistItemDto
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = "";
    public bool IsActive { get; set; }
    public bool EnableAI { get; set; }
    public decimal MinConfidence { get; set; }
    public int ScanIntervalMinutes { get; set; }
    public DateTime? LastScannedAt { get; set; }
    public int LastPatternCount { get; set; }
    public string? LastBias { get; set; }
}

public class AddWatchlistRequest
{
    public string Symbol { get; set; } = "";
    public bool EnableAI { get; set; } = true;
    public decimal MinConfidence { get; set; } = 60;
    public int ScanIntervalMinutes { get; set; } = 30;
}

public class UpdateWatchlistRequest
{
    public bool IsActive { get; set; } = true;
    public bool EnableAI { get; set; } = true;
    public decimal MinConfidence { get; set; } = 60;
    public int ScanIntervalMinutes { get; set; } = 30;
}