namespace MoneyTransfer.Application.Common.Interfaces;

public interface IRequestContextOverride
{
    Guid? OrganizationId { get; set; }

    string? UserId { get; set; }
}