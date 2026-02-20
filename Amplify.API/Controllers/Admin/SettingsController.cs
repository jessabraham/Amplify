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