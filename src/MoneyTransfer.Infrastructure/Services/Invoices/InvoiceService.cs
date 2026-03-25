using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Invoices;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Invoices;

public sealed class InvoiceService : IInvoiceService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;

    public InvoiceService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
    }

    public async Task<IReadOnlyList<InvoiceListItemDto>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var projectExists = await _dbContext.Projects
            .AnyAsync(
                x => x.Id == projectId
                  && x.OrganizationId == organizationId
                  && !x.IsArchived,
                cancellationToken);

        if (!projectExists)
        {
            return Array.Empty<InvoiceListItemDto>();
        }

        return await _dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.OrganizationId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new InvoiceListItemDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                Amount = x.Amount,
                Description = x.Description,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var projectExists = await _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == projectId
                  && x.OrganizationId == organizationId
                  && !x.IsArchived,
                cancellationToken);

        if (!projectExists)
        {
            throw new InvalidOperationException("Project was not found.");
        }

        return new InvoiceCreateDto
        {
            ProjectId = projectId
        };
    }

    public async Task CreateAsync(
        InvoiceCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var projectExists = await _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == dto.ProjectId
                  && x.OrganizationId == organizationId
                  && !x.IsArchived,
                cancellationToken);

        if (!projectExists)
        {
            throw new InvalidOperationException("Project was not found.");
        }

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ProjectId = dto.ProjectId,
            OrganizationId = organizationId,
            Amount = dto.Amount,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Invoices.Add(invoice);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid GetRequiredOrganizationId()
    {
        return _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException(
                "Current user is not associated with an organization.");
    }
}