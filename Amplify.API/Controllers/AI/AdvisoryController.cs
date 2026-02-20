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
        var riskSection = "";
        if (request.ShareCount.HasValue)
        {
            riskSection = $"""

            RISK ASSESSMENT (from Risk Engine):
            Portfolio Size: ${request.PortfolioSize:N0}
            Position Size: {request.ShareCount} shares (${request.PositionValue:N0})
            Max Loss: ${request.MaxLoss:N0} ({request.RiskPercent}% of portfolio)
            Risk/Reward Ratio: {request.RiskRewardRatio:F1}:1
            Kelly Criterion: {request.KellyPercent:F1}%
            Passes Risk Check: {(request.PassesRiskCheck == true ? "YES" : "NO")}
            Warnings: {request.Warnings ?? "None"}
            """;
        }

        var prompt = $"""
            You are Amplify AI, a trading advisory assistant that combines pattern analysis 
            with quantitative risk assessment. You give risk-aware recommendations.

            TRADE SIGNAL:
            Asset: {request.Asset}
            Signal Type: {request.SignalType}
            Market Regime: {request.Regime}
            Setup Score: {request.SetupScore}/100
            Entry Price: ${request.EntryPrice}
            Stop Loss: ${request.StopLoss}
            Target 1: ${request.Target1}
            Target 2: ${request.Target2}
            Risk: {request.RiskPercent}%
            {riskSection}

            Provide a concise analysis covering:
            1. SETUP QUALITY — Is this pattern valid in the current regime? Score assessment.
            2. RISK/REWARD — Calculate R:R. Is the reward worth the risk? Compare to Kelly sizing.
            3. POSITION SIZING — Is the share count appropriate for the portfolio? Over/under-exposed?
            4. KEY RISKS — What could invalidate this setup? What to watch.
            5. RECOMMENDATION — Strong Buy / Buy / Hold / Avoid — with a 1-sentence reason.

            If the risk check FAILS or R:R is below 1.5:1, be explicit about why this trade 
            may not be worth taking. Suggest specific adjustments (tighter stop, wider target, 
            or waiting for a better entry) with exact price levels.

            Be concise and data-driven. Use the actual numbers provided.
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

    // Risk assessment fields (optional — included when available)
    public int? ShareCount { get; set; }
    public decimal? PositionValue { get; set; }
    public decimal? MaxLoss { get; set; }
    public decimal? RiskRewardRatio { get; set; }
    public decimal? KellyPercent { get; set; }
    public bool? PassesRiskCheck { get; set; }
    public decimal? PortfolioSize { get; set; }
    public string? Warnings { get; set; }
}