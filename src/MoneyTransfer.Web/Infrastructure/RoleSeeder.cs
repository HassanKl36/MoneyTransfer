using Microsoft.AspNetCore.Identity;

namespace MoneyTransfer.Web.Infrastructure;

public static class RoleSeeder
{
    private static readonly string[] Roles =
    [
        "Admin",
        "Staff",
        "Customer"
    ];

    public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}