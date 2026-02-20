using Amplify.Application.Common.DTOs.Trading;
using Amplify.Application.Common.Interfaces.Trading;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amplify.API.Controllers.Trading;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RiskController : ControllerBase
{
    private readonly IRiskEngine _riskEngine;

    public RiskController(IRiskEngine riskEngine)
        => _riskEngine = riskEngine;

    /// <summary>
    /// Calculate full risk assessment: position sizing, R:R, Kelly, and validation.
    /// </summary>
    [HttpPost("calculate")]
    public IActionResult Calculate([FromBody] RiskInputDto input)
    {
        var result = _riskEngine.CalculateRisk(input);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}