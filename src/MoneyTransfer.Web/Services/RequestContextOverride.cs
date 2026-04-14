using MoneyTransfer.Application.Common.Interfaces;

namespace MoneyTransfer.Web.Services;

public sealed class RequestContextOverride : IRequestContextOverride
{
    public Guid? OrganizationId { get; set; }

    public string? UserId { get; set; }
}