using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Infrastructure.Identity;

namespace MoneyTransfer.Web.Services;

public sealed class CurrentOrganization : ICurrentOrganization
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRequestContextOverride _requestContextOverride;

    public CurrentOrganization(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        IRequestContextOverride requestContextOverride)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _requestContextOverride = requestContextOverride;
    }

    public async Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken = default)
    {
        if (_requestContextOverride.OrganizationId.HasValue)
        {
            return _requestContextOverride.OrganizationId.Value;
        }

        var user = _httpContextAccessor.HttpContext?.User;

        var claimValue = user?.FindFirstValue("organization_id");
        if (Guid.TryParse(claimValue, out var claimOrganizationId))
        {
            return claimOrganizationId;
        }

        var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var applicationUser = await _userManager.FindByIdAsync(userId);
        return applicationUser?.OrganizationId;
    }

    public async Task<Guid> GetRequiredOrganizationIdAsync(CancellationToken cancellationToken = default)
    {
        return await GetOrganizationIdAsync(cancellationToken)
            ?? throw new InvalidOperationException("Current user is not associated with an organization.");
    }
}