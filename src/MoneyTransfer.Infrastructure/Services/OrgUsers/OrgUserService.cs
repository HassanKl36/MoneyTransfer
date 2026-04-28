using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.OrgUsers;
using MoneyTransfer.Infrastructure.Data;
using MoneyTransfer.Infrastructure.Identity;

namespace MoneyTransfer.Infrastructure.Services.OrgUsers;

public sealed class OrgUserService : IOrgUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly MoneyTransferDbContext _dbContext;

    public OrgUserService(
        UserManager<ApplicationUser> userManager,
        ICurrentOrganization currentOrganization,
        MoneyTransferDbContext dbContext)
    {
        _userManager = userManager;
        _currentOrganization = currentOrganization;
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<OrgUserListItemDto>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetOrganizationIdAsync(cancellationToken);

        if (organizationId is null)
        {
            return [];
        }

        var users = await _userManager.Users
            .Where(u => u.OrganizationId == organizationId)
            .ToListAsync(cancellationToken);

        var customerClientIds = users
            .Where(u => u.ClientId.HasValue)
            .Select(u => u.ClientId!.Value)
            .Distinct()
            .ToList();

        var clientNamesById = customerClientIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _dbContext.Clients
                .AsNoTracking()
                .Where(c => c.OrganizationId == organizationId && customerClientIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        var result = new List<OrgUserListItemDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? string.Empty;

            var displayName = role == "Customer" && user.ClientId.HasValue
                ? clientNamesById.TryGetValue(user.ClientId.Value, out var clientName)
                    ? clientName
                    : (!string.IsNullOrWhiteSpace(user.FullName) ? user.FullName : user.Email ?? string.Empty)
                : user.FullName;

            result.Add(new OrgUserListItemDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                FullName = displayName,
                Role = role
            });
        }

        return result;
    }

    public async Task<(bool Succeeded, List<string> Errors)> CreateAsync(
        OrgUserCreateDto model,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        var organizationId = await _currentOrganization.GetOrganizationIdAsync(cancellationToken);

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