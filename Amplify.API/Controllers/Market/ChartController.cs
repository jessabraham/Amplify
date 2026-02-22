using Amplify.Application.Common.Interfaces.Market;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amplify.API.Controllers.Market;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChartController : ControllerBase
{
    private readonly IMarketDataService _marketData;

    public ChartController(IMarketDataService marketData)
    {
        _marketData = marketData;
    }

    /// <summary>
    /// Get OHLCV candle data for charting.
    /// </summary>
    [HttpGet("candles")]
    public async Task<IActionResult> GetCandles(
        [FromQuery] string symbol,
        [FromQuery] int count = 100,
        [FromQuery] string timeframe = "1H")
    {
        if (string.IsNullOrEmpty(symbol))
            return BadRequest("Symbol is required.");

        count = Math.Clamp(count, 10, 500);

        try
        {
            var candles = await _marketData.GetCandlesAsync(symbol, count, timeframe);
            var result = candles.Select(c => new
            {
                time = new DateTimeOffset(c.Time, TimeSpan.Zero).ToUnixTimeSeconds(),
                open = Math.Round(c.Open, 2),
                high = Math.Round(c.High, 2),
                low = Math.Round(c.Low, 2),
                close = Math.Round(c.Close, 2),
                volume = Math.Round(c.Volume, 0)
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }
}