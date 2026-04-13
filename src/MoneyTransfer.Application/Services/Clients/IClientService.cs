using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Application.Services.Clients;

public interface IClientService
{
    Task<IReadOnlyList<ClientListItemDto>> GetClientsAsync(
        string? search = null,
        ClientStatus? status = null,
        CancellationToken cancellationToken = default);

    Task<ClientEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, List<string> Errors)> CreateAsync(
        ClientEditDto model,
        CancellationToken cancellationToken = default);

    Task<(bool Succeeded, List<string> Errors)> UpdateAsync(
        ClientEditDto model,
        CancellationToken cancellationToken = default);

    Task<bool> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ClientDetailsDto?> GetDetailsAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);
}