using Amplify.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amplify.Infrastructure.Persistence.Seeders;

public static class AdminSeeder
{
    public static async Task SeedAdminAsync(IServiceProvider sp)
    {
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var config = sp.GetRequiredService<IConfiguration>();

        var email = config["AdminSeed:Email"]!;
        var displayName = config["AdminSeed:DisplayName"]!;
        var password = config["AdminSeed:Password"]!;

        if (await userManager.FindByEmailAsync(email) is not null)
            return; // Already seeded

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);

        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, "Admin");
    }
}