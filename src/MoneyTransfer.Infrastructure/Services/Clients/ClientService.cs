using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Clients;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Clients;

public sealed class ClientService : IClientService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;

    public ClientService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
    }

    public async Task<IReadOnlyList<ClientListItemDto>> GetClientsAsync(
        bool includeArchived = false,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var query = _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId);

        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        return await query
            .OrderBy(c => c.Name)
            .Select(c => new ClientListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                IsArchived = c.IsArchived
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientEditDto?> GetForEditAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        return await _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.OrganizationId == organizationId && c.Id == id)
            .Select(c => new ClientEditDto
            {
                Id = c.Id,
                Name = c.Name,
                IsArchived = c.IsArchived
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task CreateAsync(
        ClientEditDto model,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var client = new Domain.Entities.Client
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = model.Name.Trim(),
            IsArchived = false
        };

        _dbContext.Clients.Add(client);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        ClientEditDto model,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId && c.Id == model.Id,
                cancellationToken);

        if (client is null)
        {
            return false;
        }

        client.Name = model.Name.Trim();
        client.IsArchived = model.IsArchived;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ArchiveAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var client = await _dbContext.Clients
            .FirstOrDefaultAsync(
                c => c.OrganizationId == organizationId && c.Id == id,
                cancellationToken);

        if (client is null)
        {
            return false;
        }

        client.IsArchived = true;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private Guid GetRequiredOrganizationId()
    {
        return _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException(
                "Current user is not associated with an organization.");
    }
}