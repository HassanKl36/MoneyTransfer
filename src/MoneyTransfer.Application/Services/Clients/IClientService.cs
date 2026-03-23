namespace MoneyTransfer.Application.Services.Clients;

public interface IClientService
{
    Task<IReadOnlyList<ClientListItemDto>> GetClientsAsync(bool includeArchived = false, CancellationToken cancellationToken = default);
    Task<ClientEditDto?> GetForEditAsync(Guid id, CancellationToken cancellationToken = default);
    Task CreateAsync(ClientEditDto model, CancellationToken cancellationToken = default);
    Task<bool> UpdateAsync(ClientEditDto model, CancellationToken cancellationToken = default);
    Task<bool> ArchiveAsync(Guid id, CancellationToken cancellationToken = default);
}