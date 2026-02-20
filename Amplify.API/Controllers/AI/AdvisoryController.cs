using Amplify.Application.Common.Interfaces.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Amplify.API.Controllers.AI;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdvisoryController : ControllerBase
{
    private readonly IAIAdvisor _advisor;

    public AdvisoryController(IAIAdvisor advisor)
        => _advisor = advisor;

    [HttpPost("chat")]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        var systemPrompt = """
            You are Amplify AI, a trading advisory assistant. You help traders analyze 
            market setups, evaluate trade signals, assess risk, and make informed decisions.
            
            When analyzing a trade, consider:
            - Market regime (trending, choppy, volatility expansion, mean reversion)
            - Setup quality and score
            - Risk/reward ratio
            - Entry, stop loss, and target levels
            - Position sizing based on risk percentage
            
            Be concise, data-driven, and actionable. Use specific numbers when possible.
            Format key levels and recommendations clearly.
            Do not provide financial advice — frame everything as analysis and education.
            """;

        var fullPrompt = $"{systemPrompt}\n\nUser: {request.Message}";

        try
        {
            var response = await _advisor.GetAdvisoryAsync(fullPrompt);
            return Ok(new { Response = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = $"AI service unavailable: {ex.Message}" });
        }
    }

    [HttpPost("analyze-signal")]
    public async Task<IActionResult> AnalyzeSignal([FromBody] SignalAnalysisRequest request)
    {
        var prompt = $"""
            You are Amplify AI, a trading advisory assistant. Analyze this trade signal:

            Asset: {request.Asset}
            Signal Type: {request.SignalType}
            Market Regime: {request.Regime}
            Setup Score: {request.SetupScore}/100
            Entry Price: ${request.EntryPrice}
            Stop Loss: ${request.StopLoss}
            Target 1: ${request.Target1}
            Target 2: ${request.Target2}
            Risk: {request.RiskPercent}%

            Provide:
            1. Risk/Reward analysis (calculate R:R ratio for both targets)
            2. Setup quality assessment based on the score and regime
            3. Position sizing suggestion (assuming $100,000 portfolio)
            4. Key risks and what to watch for
            5. Overall recommendation: Strong conviction / Moderate / Weak — and why

            Be concise and data-driven. Use bullet points.
            """;

        try
        {
            var response = await _advisor.GetAdvisoryAsync(prompt);
            return Ok(new { Response = response });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = $"AI service unavailable: {ex.Message}" });
        }
    }
}

public class ChatRequest
{
    public string Message { get; set; } = "";
}

public class SignalAnalysisRequest
{
    public string Asset { get; set; } = "";
    public string SignalType { get; set; } = "";
    public string Regime { get; set; } = "";
    public decimal SetupScore { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target1 { get; set; }
    public decimal Target2 { get; set; }
    public decimal RiskPercent { get; set; }
}