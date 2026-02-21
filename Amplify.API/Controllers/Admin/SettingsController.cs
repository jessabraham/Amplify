using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;
using Amplify.Domain.Entities.Identity;

namespace Amplify.API.Controllers.Admin;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public SettingsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IConfiguration config)
    {
        _context = context;
        _userManager = userManager;
        _config = config;
    }

    // ===== USER PROFILE =====

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new
        {
            user.DisplayName,
            user.Email,
            user.UserName,
            Role = roles.FirstOrDefault() ?? "Viewer",
            user.CreatedAt,
            user.LastLoginUtc
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            user.Email = request.Email.Trim();
            user.UserName = request.Email.Trim();
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.First().Description);

        return Ok();
    }

    [HttpPut("password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.First().Description);

        return Ok();
    }

    // ===== AI MODEL CONFIG =====

    [HttpGet("ai")]
    public IActionResult GetAIConfig()
    {
        return Ok(new
        {
            BaseUrl = _config["Ollama:BaseUrl"] ?? "http://localhost:11434",
            Model = _config["Ollama:Model"] ?? "qwen3:8b",
            Temperature = double.TryParse(_config["Ollama:Temperature"], out var t) ? t : 0.7,
            MaxTokens = int.TryParse(_config["Ollama:MaxTokens"], out var m) ? m : 2048
        });
    }

    // ===== PORTFOLIO BALANCE =====

    [HttpGet("portfolio-balance")]
    public async Task<IActionResult> GetPortfolioBalance()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        // Calculate invested (open position costs)
        var totalInvested = 0m;
        var totalUnrealizedPnL = 0m;
        try
        {
            var openPositions = await _context.Positions
                .Where(p => p.UserId == userId && p.Status == Domain.Enumerations.PositionStatus.Open && p.IsActive)
                .ToListAsync();

            totalInvested = openPositions.Sum(p => p.EntryPrice * p.Quantity);
            totalUnrealizedPnL = openPositions.Sum(p => p.UnrealizedPnL);
        }
        catch { /* Positions table may not exist yet */ }

        // Realized P&L from resolved simulated trades
        var realizedPnL = 0m;
        try
        {
            realizedPnL = await _context.SimulatedTrades
                .Where(t => t.UserId == userId && t.Status == Domain.Enumerations.SimulationStatus.Resolved)
                .SumAsync(t => t.PnLDollars ?? 0);
        }
        catch { }

        var portfolioValue = user.StartingCapital + realizedPnL + totalUnrealizedPnL;
        var cashAvailable = user.StartingCapital + realizedPnL - totalInvested;

        // AI Trading Budget calculations
        var aiBudgetDollars = Math.Round(cashAvailable * user.AiTradingBudgetPercent / 100m, 2);
        // How much of the AI budget is currently invested in AI-created positions
        var aiInvested = await _context.Positions
            .Where(p => p.UserId == userId && p.Status == Domain.Enumerations.PositionStatus.Open && p.IsActive && p.IsAiGenerated)
            .SumAsync(p => p.EntryPrice * p.Quantity);

        return Ok(new PortfolioBalanceDto
        {
            StartingCapital = user.StartingCapital,
            RealizedPnL = Math.Round(realizedPnL, 2),
            UnrealizedPnL = Math.Round(totalUnrealizedPnL, 2),
            PortfolioValue = Math.Round(portfolioValue, 2),
            TotalInvested = Math.Round(totalInvested, 2),
            CashAvailable = Math.Round(Math.Max(cashAvailable, 0), 2),
            OpenPositionCount = (int)(totalInvested > 0 ? await _context.Positions
                .CountAsync(p => p.UserId == userId && p.Status == Domain.Enumerations.PositionStatus.Open && p.IsActive) : 0),
            AiTradingBudgetPercent = user.AiTradingBudgetPercent,
            AiTradingBudgetDollars = Math.Max(aiBudgetDollars, 0),
            AiCashUsed = Math.Round(aiInvested, 2),
            AiCashRemaining = Math.Round(Math.Max(aiBudgetDollars - aiInvested, 0), 2)
        });
    }

    [HttpPut("portfolio-balance")]
    public async Task<IActionResult> UpdateStartingCapital([FromBody] UpdateCapitalRequest request)
    {
        if (request.StartingCapital < 100)
            return BadRequest("Starting capital must be at least $100.");
        if (request.StartingCapital > 100_000_000)
            return BadRequest("Starting capital cannot exceed $100,000,000.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        user.StartingCapital = request.StartingCapital;
        await _userManager.UpdateAsync(user);

        return Ok(new { user.StartingCapital });
    }

    [HttpPut("ai-budget")]
    public async Task<IActionResult> UpdateAiBudget([FromBody] UpdateAiBudgetRequest request)
    {
        if (request.AiTradingBudgetPercent < 0)
            return BadRequest("AI budget cannot be negative.");
        if (request.AiTradingBudgetPercent > 100)
            return BadRequest("AI budget cannot exceed 100%.");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        user.AiTradingBudgetPercent = request.AiTradingBudgetPercent;
        await _userManager.UpdateAsync(user);

        return Ok(new { user.AiTradingBudgetPercent });
    }

    // ===== RISK DEFAULTS =====

    [HttpGet("risk")]
    public IActionResult GetRiskConfig()
    {
        return Ok(new
        {
            DefaultRiskPercent = double.TryParse(_config["Risk:DefaultRiskPercent"], out var r) ? r : 2.0,
            MaxRiskPercent = double.TryParse(_config["Risk:MaxRiskPercent"], out var mr) ? mr : 5.0,
            MaxPositionSize = int.TryParse(_config["Risk:MaxPositionSize"], out var mp) ? mp : 50000,
            DefaultPortfolioSize = int.TryParse(_config["Risk:DefaultPortfolioSize"], out var dp) ? dp : 100000
        });
    }
}

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = "";
    public string NewPassword { get; set; } = "";
}

public class UpdateCapitalRequest
{
    public decimal StartingCapital { get; set; }
}

public class UpdateAiBudgetRequest
{
    public decimal AiTradingBudgetPercent { get; set; }
}

public class PortfolioBalanceDto
{
    public decimal StartingCapital { get; set; }
    public decimal RealizedPnL { get; set; }
    public decimal UnrealizedPnL { get; set; }
    public decimal PortfolioValue { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CashAvailable { get; set; }
    public int OpenPositionCount { get; set; }
    public decimal AiTradingBudgetPercent { get; set; }
    public decimal AiTradingBudgetDollars { get; set; }
    public decimal AiCashUsed { get; set; }
    public decimal AiCashRemaining { get; set; }
}