using System.Security.Claims;
using MoneyTransfer.Application.Common.Interfaces;

namespace MoneyTransfer.Web.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRequestContextOverride _requestContextOverride;

    public CurrentUser(
        IHttpContextAccessor httpContextAccessor,
        IRequestContextOverride requestContextOverride)
    {
        _httpContextAccessor = httpContextAccessor;
        _requestContextOverride = requestContextOverride;
    }

    public string? UserId =>
        _requestContextOverride.UserId
        ?? _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
}