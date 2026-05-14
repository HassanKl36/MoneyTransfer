using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Infrastructure.Data;
using MoneyTransfer.Infrastructure.Identity;

namespace MoneyTransfer.Web.Infrastructure;

public static class ApplicationDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var serviceProvider = scope.ServiceProvider;
        var dbContext = serviceProvider.GetRequiredService<MoneyTransferDbContext>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ApplicationDbInitializer));

        logger.LogInformation("Applying database migrations...");
        await dbContext.Database.MigrateAsync();

        logger.LogInformation("Seeding roles...");
        await RoleSeeder.SeedAsync(roleManager);

        logger.LogInformation("Seeding default admin account if needed...");
        await SeedDefaultAdminAsync(dbContext, userManager, configuration, logger);
    }

    private static async Task SeedDefaultAdminAsync(
        MoneyTransferDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        ILogger logger)
    {
        var seedSection = configuration.GetSection("SeedAdmin");

        var organizationName = seedSection["OrganizationName"]?.Trim();
        var organizationCode = seedSection["OrganizationCode"]?.Trim();
        var fullName = seedSection["FullName"]?.Trim();
        var email = seedSection["Email"]?.Trim();
        var password = seedSection["Password"];

        if (string.IsNullOrWhiteSpace(organizationName) ||
            string.IsNullOrWhiteSpace(organizationCode) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "SeedAdmin settings are incomplete. Default admin account was not seeded.");

            return;
        }

        organizationCode = organizationCode.ToUpperInvariant();

        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            logger.LogInformation(
                "Default admin user already exists. Skipping admin seed.");

            return;
        }

        var organization = await dbContext.Organizations
            .FirstOrDefaultAsync(x => x.Code == organizationCode);

        if (organization is null)
        {
            organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = organizationName,
                Code = organizationCode,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "system-seed"
            };

            dbContext.Organizations.Add(organization);

            dbContext.OrganizationSequences.Add(new OrganizationSequence
            {
                OrganizationId = organization.Id,
                NextProjectNumber = 1,
                NextInvoiceNumber = 1,
                NextPaymentNumber = 1,
                NextDiscountNumber = 1
            });

            await dbContext.SaveChangesAsync();

            logger.LogInformation(
                "Seeded default organization with code {OrganizationCode}.",
                organizationCode);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = fullName,
            OrganizationId = organization.Id
        };

        var createUserResult = await userManager.CreateAsync(user, password);
        if (!createUserResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                createUserResult.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"Failed to seed default admin user: {errors}");
        }

        var addRoleResult = await userManager.AddToRoleAsync(user, "Admin");
        if (!addRoleResult.Succeeded)
        {
            var errors = string.Join(
                "; ",
                addRoleResult.Errors.Select(x => x.Description));

            throw new InvalidOperationException(
                $"Failed to assign Admin role to default admin user: {errors}");
        }

        organization.CreatedBy = user.Id;
        await dbContext.SaveChangesAsync();

        logger.LogInformation(
            "Seeded default admin account {Email}.",
            email);
    }
}