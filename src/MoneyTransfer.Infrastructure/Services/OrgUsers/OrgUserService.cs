using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.OrgUsers;
using MoneyTransfer.Infrastructure.Identity;

namespace MoneyTransfer.Infrastructure.Services.OrgUsers;

public sealed class OrgUserService : IOrgUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentOrganization _currentOrganization;

    public OrgUserService(
        UserManager<ApplicationUser> userManager,
        ICurrentOrganization currentOrganization)
    {
        _userManager = userManager;
        _currentOrganization = currentOrganization;
    }

    public async Task<IReadOnlyList<OrgUserListItemDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var organizationId = _currentOrganization.OrganizationId;

        if (organizationId is null)
        {
            return [];
        }

        var users = await _userManager.Users
            .Where(u => u.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        var result = new List<OrgUserListItemDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);

            result.Add(new OrgUserListItemDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                Role = roles.FirstOrDefault() ?? string.Empty
            });
        }

        return result;
    }

    public async Task<(bool Succeeded, List<string> Errors)> CreateAsync(
        OrgUserCreateDto model,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        var organizationId = _currentOrganization.OrganizationId;

        if (organizationId is null)
        {
            errors.Add("No organization context found.");
            return (false, errors);
        }

        var email = model.Email.Trim();
        var fullName = model.FullName.Trim();
        var role = model.Role?.Trim();

        if (string.IsNullOrWhiteSpace(email))
            errors.Add("Email is required.");

        if (string.IsNullOrWhiteSpace(fullName))
            errors.Add("Full name is required.");

        if (string.IsNullOrWhiteSpace(model.Password))
            errors.Add("Password is required.");

        if (role != "Admin" && role != "Staff")
            errors.Add("Invalid role selected.");

        if (errors.Count > 0)
            return (false, errors);

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            errors.Add("A user with this email already exists.");
            return (false, errors);
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            OrganizationId = organizationId
        };

        var createResult = await _userManager.CreateAsync(user, model.Password);

        if (!createResult.Succeeded)
        {
            errors.AddRange(createResult.Errors.Select(e => e.Description));
            return (false, errors);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role!);

        if (!roleResult.Succeeded)
        {
            errors.AddRange(roleResult.Errors.Select(e => e.Description));
            return (false, errors);
        }

        return (true, errors);
    }
}