using Amplify.Application.Common.Interfaces.Trading;
using Amplify.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amplify.API.Controllers.Market;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RegimeController : ControllerBase
{
    private readonly IRegimeService _regimeService;

    public RegimeController(IRegimeService regimeService)
        => _regimeService = regimeService;

    /// <summary>
    /// Detect market regime from candle data.
    /// POST body: array of Candle objects (need 50+ candles).
    /// </summary>
    [HttpPost("detect")]
    public async Task<IActionResult> Detect([FromQuery] string symbol, [FromBody] List<Candle> candles)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return BadRequest("Symbol is required.");

        var result = await _regimeService.DetectAndStoreAsync(symbol.ToUpper(), candles);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    /// <summary>
    /// Get the most recent regime for a symbol.
    /// </summary>
    [HttpGet("{symbol}/latest")]
    public async Task<IActionResult> GetLatest(string symbol)
    {
        var result = await _regimeService.GetLatestRegimeAsync(symbol.ToUpper());
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    /// <summary>
    /// Get regime detection history for a symbol.
    /// </summary>
    [HttpGet("{symbol}/history")]
    public async Task<IActionResult> GetHistory(string symbol, [FromQuery] int days = 30)
    {
        var result = await _regimeService.GetHistoryAsync(symbol.ToUpper(), days);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}