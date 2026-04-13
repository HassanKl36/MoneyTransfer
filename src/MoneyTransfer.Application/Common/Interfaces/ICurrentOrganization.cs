namespace MoneyTransfer.Application.Common.Interfaces;

public interface ICurrentOrganization
{
    Task<Guid?> GetOrganizationIdAsync(CancellationToken cancellationToken = default);

    Task<Guid> GetRequiredOrganizationIdAsync(CancellationToken cancellationToken = default);
}