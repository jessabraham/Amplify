using Amplify.Domain.Entities.Trading;
using Amplify.Domain.Enumerations;
using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Amplify.API.Controllers.Trading;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OverridesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OverridesController(ApplicationDbContext context)
        => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetOverrides()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var overrides = await _context.UserOverrides
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.TradeSignalId,
                Asset = o.TradeSignal.Asset,
                SignalType = o.TradeSignal.SignalType.ToString(),
                OverrideType = o.OverrideType.ToString(),
                Reason = o.Reason.ToString(),
                o.Notes,
                o.ModifiedEntryPrice,
                o.ModifiedStopLoss,
                o.ModifiedTarget1,
                o.ModifiedTarget2,
                o.ActualPnL,
                o.WasCorrect,
                o.CreatedAt
            })
            .ToListAsync();

        return Ok(overrides);
    }

    [HttpPost]
    public async Task<IActionResult> CreateOverride([FromBody] CreateOverrideRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var signal = await _context.TradeSignals.FindAsync(request.TradeSignalId);
        if (signal is null)
            return NotFound("Signal not found.");

        var entity = new UserOverride
        {
            TradeSignalId = request.TradeSignalId,
            UserId = userId,
            OverrideType = request.OverrideType,
            Reason = request.Reason,
            Notes = request.Notes,
            ModifiedEntryPrice = request.ModifiedEntryPrice,
            ModifiedStopLoss = request.ModifiedStopLoss,
            ModifiedTarget1 = request.ModifiedTarget1,
            ModifiedTarget2 = request.ModifiedTarget2
        };

        _context.UserOverrides.Add(entity);
        await _context.SaveChangesAsync();

        return Ok(new { entity.Id });
    }

    [HttpPut("{id}/outcome")]
    public async Task<IActionResult> UpdateOutcome(Guid id, [FromBody] OutcomeRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var entity = await _context.UserOverrides
            .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

        if (entity is null)
            return NotFound();

        entity.ActualPnL = request.ActualPnL;
        entity.WasCorrect = request.WasCorrect;
        entity.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok();
    }
}

public class CreateOverrideRequest
{
    public Guid TradeSignalId { get; set; }
    public OverrideType OverrideType { get; set; }
    public OverrideReason Reason { get; set; }
    public string? Notes { get; set; }
    public decimal? ModifiedEntryPrice { get; set; }
    public decimal? ModifiedStopLoss { get; set; }
    public decimal? ModifiedTarget1 { get; set; }
    public decimal? ModifiedTarget2 { get; set; }
}

public class OutcomeRequest
{
    public decimal ActualPnL { get; set; }
    public bool WasCorrect { get; set; }
}