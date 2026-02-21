using System.Security.Claims;
using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amplify.API.Controllers.Trading;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfolioController(IPortfolioService portfolioService)
        => _portfolioService = portfolioService;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── Summary ─────────────────────────────────────────────────────

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var result = await _portfolioService.GetPortfolioSummaryAsync(UserId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // ── Positions ───────────────────────────────────────────────────

    [HttpGet("positions/open")]
    public async Task<IActionResult> GetOpenPositions()
    {
        var result = await _portfolioService.GetOpenPositionsAsync(UserId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("positions/closed")]
    public async Task<IActionResult> GetClosedPositions()
    {
        var result = await _portfolioService.GetClosedPositionsAsync(UserId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("positions/{id}")]
    public async Task<IActionResult> GetPosition(Guid id)
    {
        var result = await _portfolioService.GetPositionByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost("positions")]
    public async Task<IActionResult> OpenPosition([FromBody] PositionDto dto)
    {
        var result = await _portfolioService.OpenPositionAsync(dto, UserId);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetPosition), new { id = result.Value!.Id }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpPost("positions/close")]
    public async Task<IActionResult> ClosePosition([FromBody] ClosePositionDto dto)
    {
        var result = await _portfolioService.ClosePositionAsync(dto, UserId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpPut("positions/{id}/price")]
    public async Task<IActionResult> UpdatePrice(Guid id, [FromQuery] decimal price)
    {
        var result = await _portfolioService.UpdateCurrentPriceAsync(id, price);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpDelete("positions/{id}")]
    public async Task<IActionResult> DeletePosition(Guid id)
    {
        var result = await _portfolioService.DeletePositionAsync(id, UserId);
        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    // ── Snapshots ───────────────────────────────────────────────────

    [HttpPost("snapshots")]
    public async Task<IActionResult> TakeSnapshot()
    {
        var result = await _portfolioService.TakeSnapshotAsync(UserId);
        return result.IsSuccess ? Ok() : BadRequest(result.Error);
    }

    [HttpGet("snapshots")]
    public async Task<IActionResult> GetSnapshots([FromQuery] int days = 30)
    {
        var result = await _portfolioService.GetSnapshotsAsync(UserId, days);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}