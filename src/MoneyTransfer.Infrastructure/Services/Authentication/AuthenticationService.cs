using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Services.Authentication;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Infrastructure.Data;
using MoneyTransfer.Infrastructure.Identity;

namespace MoneyTransfer.Infrastructure.Services.Authentication;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuthenticationService(
        MoneyTransferDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<RegisterOrganizationResult> RegisterOrganizationAsync(RegisterOrganizationRequest request)
    {
        var result = new RegisterOrganizationResult();

        var email = request.Email.Trim();
        var organizationName = request.OrganizationName.Trim();
        var organizationCode = request.OrganizationCode?.Trim();
        var fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(organizationName))
        {
            result.Errors.Add("Organization name is required.");
        }

        if (string.IsNullOrWhiteSpace(organizationCode))
        {
            result.Errors.Add("Organization code is required.");
        }

        if (string.IsNullOrWhiteSpace(fullName))
        {
            result.Errors.Add("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            result.Errors.Add("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            result.Errors.Add("Password is required.");
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        organizationCode = organizationCode!.ToUpperInvariant();

        var emailExists = await _userManager.FindByEmailAsync(email);
        if (emailExists is not null)
        {
            result.Errors.Add("A user with this email already exists.");
            return result;
        }

        var normalizedOrganizationName = organizationName.ToUpperInvariant();
        var organizationExists = await _dbContext.Organizations
            .AnyAsync(x => x.Name.ToUpper() == normalizedOrganizationName);

        if (organizationExists)
        {
            result.Errors.Add("An organization with this name already exists.");
            return result;
        }

        var codeExists = await _dbContext.Organizations
            .AnyAsync(x => x.Code.ToUpper() == organizationCode);

        if (codeExists)
        {
            result.Errors.Add("An organization with this code already exists.");
            return result;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var organization = new Organization
            {
                Id = Guid.NewGuid(),
                Name = organizationName,
                Code = organizationCode,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = string.Empty
            };

            _dbContext.Organizations.Add(organization);
            await _dbContext.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                OrganizationId = organization.Id
            };

            var createUserResult = await _userManager.CreateAsync(user, request.Password);

            if (!createUserResult.Succeeded)
            {
                result.Errors.AddRange(createUserResult.Errors.Select(e => e.Description));
                await transaction.RollbackAsync();
                return result;
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, "Admin");

            if (!addRoleResult.Succeeded)
            {
                result.Errors.AddRange(addRoleResult.Errors.Select(e => e.Description));
                await transaction.RollbackAsync();
                return result;
            }

            organization.CreatedBy = user.Id;
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            result.Succeeded = true;
            result.OrganizationId = organization.Id;
            result.UserId = user.Id;

            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}