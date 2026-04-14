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
    private readonly ICurrentUser _currentUser;
    private readonly IFinancialIdentityGenerator _financialIdentityGenerator;

    public InvoiceService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization,
        ICurrentUser currentUser,
        IFinancialIdentityGenerator financialIdentityGenerator)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _financialIdentityGenerator = financialIdentityGenerator;
    }

    public async Task<IReadOnlyList<InvoiceListItemDto>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

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
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

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

    public async Task<InvoiceCreateResultDto> CreateAsync(
        InvoiceCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == dto.ProjectId
                  && x.OrganizationId == organizationId
                  && x.Status != ProjectStatus.Archived,
                cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("Project was not found.");
        }

        var invoiceNumber = await _financialIdentityGenerator.GenerateInvoiceNumberAsync(
            organizationId,
            cancellationToken);

        var now = DateTime.UtcNow;
        var userId = GetRequiredUserId();
        var description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();

        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            OrganizationId = organizationId,
            InvoiceNumber = invoiceNumber,
            Amount = dto.Amount,
            Date = dto.Date,
            Description = description,
            CreatedAt = now,
            CreatedBy = userId
        };

        var ledgerEntry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ClientId = project.ClientId,
            ProjectId = project.Id,
            Type = LedgerEntryType.Invoice,
            Amount = dto.Amount,
            OccurredAt = dto.Date,
            Notes = description,
            InvoiceNumber = invoiceNumber,
            IsVoided = false,
            VoidedAt = null,
            CreatedAt = now,
            CreatedBy = userId
        };

        _dbContext.Invoices.Add(invoice);
        _dbContext.LedgerEntries.Add(ledgerEntry);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new InvoiceCreateResultDto
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber
        };
    }

    private string GetRequiredUserId()
    {
        return _currentUser.UserId
            ?? throw new InvalidOperationException(
                "Current user is not authenticated.");
    }
}