using Amplify.Domain.Entities.Identity;
using Amplify.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Amplify.API.Controllers.Admin;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public UsersController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userManager.Users
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        var userList = new List<object>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            userList.Add(new
            {
                user.Id,
                user.DisplayName,
                user.Email,
                Role = roles.FirstOrDefault() ?? "Viewer",
                user.CreatedAt,
                user.LastLoginUtc,
                user.LockoutEnd,
                IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow
            });
        }

        return Ok(userList);
    }

    [HttpPost("invite")]
    public async Task<IActionResult> InviteUser([FromBody] InviteUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email is required.");

        var existing = await _userManager.FindByEmailAsync(request.Email);
        if (existing is not null)
            return BadRequest("A user with this email already exists.");

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            DisplayName = request.DisplayName?.Trim() ?? request.Email.Split('@')[0],
            CreatedAt = DateTime.UtcNow
        };

        // Generate a temporary password
        var tempPassword = $"Amp!{Guid.NewGuid().ToString()[..8]}";
        var result = await _userManager.CreateAsync(user, tempPassword);

        if (!result.Succeeded)
            return BadRequest(result.Errors.First().Description);

        // Assign role
        var role = request.Role ?? "Viewer";
        if (await _roleManager.RoleExistsAsync(role))
            await _userManager.AddToRoleAsync(user, role);

        return Ok(new
        {
            user.Id,
            user.Email,
            user.DisplayName,
            Role = role,
            TempPassword = tempPassword
        });
    }

    [HttpPut("{id}/role")]
    public async Task<IActionResult> ChangeRole(string id, [FromBody] ChangeRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound("User not found.");

        // Remove existing roles
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);

        // Add new role
        if (await _roleManager.RoleExistsAsync(request.Role))
            await _userManager.AddToRoleAsync(user, request.Role);

        return Ok();
    }

    [HttpPut("{id}/lock")]
    public async Task<IActionResult> LockUser(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (id == currentUserId)
            return BadRequest("You cannot lock your own account.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound("User not found.");

        // Lock for 100 years (effectively permanent)
        await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        return Ok();
    }

    [HttpPut("{id}/unlock")]
    public async Task<IActionResult> UnlockUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound("User not found.");

        await _userManager.SetLockoutEndDateAsync(user, null);
        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (id == currentUserId)
            return BadRequest("You cannot delete your own account.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
            return NotFound("User not found.");

        // Check if this is the last admin
        var admins = await _userManager.GetUsersInRoleAsync("Admin");
        var userRoles = await _userManager.GetRolesAsync(user);
        if (userRoles.Contains("Admin") && admins.Count <= 1)
            return BadRequest("Cannot delete the last admin account.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.First().Description);

        return Ok();
    }
}

public class InviteUserRequest
{
    public string Email { get; set; } = "";
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
}

public class ChangeRoleRequest
{
    public string Role { get; set; } = "";
}