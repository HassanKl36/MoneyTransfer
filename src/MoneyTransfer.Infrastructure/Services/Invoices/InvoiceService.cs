using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Invoices;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Domain.Enums;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Invoices;

public sealed class InvoiceService : IInvoiceService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IFinancialIdentityGenerator _financialIdentityGenerator;

    public InvoiceService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization,
        IFinancialIdentityGenerator financialIdentityGenerator)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
        _financialIdentityGenerator = financialIdentityGenerator;
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
                  && x.Status != ProjectStatus.Archived,
                cancellationToken);

        if (!projectExists)
        {
            return Array.Empty<InvoiceListItemDto>();
        }

        return await _dbContext.Invoices
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.OrganizationId == organizationId)
            .OrderByDescending(x => x.Date)
            .Select(x => new InvoiceListItemDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                InvoiceNumber = x.InvoiceNumber,
                Amount = x.Amount,
                Date = x.Date,
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
                  && x.Status != ProjectStatus.Archived,
                cancellationToken);

        if (!projectExists)
        {
            throw new InvalidOperationException("Project was not found.");
        }

        return new InvoiceCreateDto
        {
            ProjectId = projectId,
            Date = DateTime.Today
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
                  && x.Status != ProjectStatus.Archived,
                cancellationToken);

        if (!projectExists)
        {
            throw new InvalidOperationException("Project was not found.");
        }

        var invoiceNumber = await _financialIdentityGenerator.GenerateInvoiceNumberAsync(
            organizationId,
            cancellationToken);

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ProjectId = dto.ProjectId,
            OrganizationId = organizationId,
            InvoiceNumber = invoiceNumber,
            Amount = dto.Amount,
            Date = dto.Date,
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
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