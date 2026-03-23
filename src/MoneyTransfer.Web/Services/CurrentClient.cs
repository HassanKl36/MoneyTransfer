using System.Security.Claims;
using MoneyTransfer.Application.Common.Interfaces;

namespace MoneyTransfer.Web.Services;

public sealed class CurrentClient : ICurrentClient
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentClient(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? ClientId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("client_id");

            if (Guid.TryParse(value, out var clientId))
            {
                return clientId;
            }

            return null;
        }
    }
}