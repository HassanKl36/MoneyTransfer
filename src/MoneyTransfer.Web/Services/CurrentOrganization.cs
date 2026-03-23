using System.Security.Claims;
using MoneyTransfer.Application.Common.Interfaces;

namespace MoneyTransfer.Web.Services;

public sealed class CurrentOrganization : ICurrentOrganization
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentOrganization(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? OrganizationId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("organization_id");

            if (Guid.TryParse(value, out var organizationId))
            {
                return organizationId;
            }

            return null;
        }
    }
}