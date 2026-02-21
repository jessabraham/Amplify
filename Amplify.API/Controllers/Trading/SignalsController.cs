using System.Security.Claims;
using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Domain.Enumerations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amplify.API.Controllers.Trading;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SignalsController : ControllerBase
{
    private readonly ITradeSignalService _signalService;

    public SignalsController(ITradeSignalService signalService)
        => _signalService = signalService;

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet]
    public async Task<IActionResult> GetSignals(
        [FromQuery] SignalSource? source = null,
        [FromQuery] SignalStatus? status = null)
    {
        var result = await _signalService.GetSignalsAsync(UserId, source, status);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSignal(Guid id)
    {
        var result = await _signalService.GetSignalByIdAsync(id);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSignal([FromBody] TradeSignalDto dto)
    {
        var result = await _signalService.CreateSignalAsync(dto, UserId);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetSignal), new { id = result.Value!.Id }, result.Value)
            : BadRequest(result.Error);
    }

    [HttpPut("{id}/accept")]
    public async Task<IActionResult> AcceptSignal(Guid id)
    {
        var result = await _signalService.AcceptSignalAsync(id, UserId);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> RejectSignal(Guid id)
    {
        var result = await _signalService.RejectSignalAsync(id);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpPut("{id}/archive")]
    public async Task<IActionResult> ArchiveSignal(Guid id)
    {
        var result = await _signalService.ArchiveSignalAsync(id);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSignal(Guid id)
    {
        var result = await _signalService.DeleteSignalAsync(id, UserId);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllSignals()
    {
        var result = await _signalService.ClearAllSignalsAsync(UserId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var result = await _signalService.GetSignalStatsAsync(UserId);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}