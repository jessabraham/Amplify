using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Amplify.Infrastructure.Persistence.Seeders;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = { "Admin", "Analyst", "Viewer" };

        foreach (var role in roles)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }
}
