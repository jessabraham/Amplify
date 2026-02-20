using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Amplify.Application.Common.DTOs.Admin;
using Amplify.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Amplify.API.Controllers.Admin;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public AuthController(UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _userManager = userManager;
        _config = config;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null)
            return Unauthorized("Invalid credentials");

        var validPassword = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!validPassword)
            return Unauthorized("Invalid credentials");

        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Viewer";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:ExpiryInMinutes"]!));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds
        );

        return Ok(new AuthResponseDto
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            Email = user.Email!,
            DisplayName = user.DisplayName,
            Role = role,
            Expiry = expiry
        });
    }
}