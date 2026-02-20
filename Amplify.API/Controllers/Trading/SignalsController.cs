using System.Security.Claims;
using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
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
    public async Task<IActionResult> GetSignals()
    {
        var result = await _signalService.GetActiveSignalsAsync(UserId);
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

    [HttpPut("{id}/archive")]
    public async Task<IActionResult> ArchiveSignal(Guid id)
    {
        var result = await _signalService.ArchiveSignalAsync(id);
        return result.IsSuccess ? Ok() : NotFound(result.Error);
    }
}