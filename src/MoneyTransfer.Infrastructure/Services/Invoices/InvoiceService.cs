using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Exceptions;
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
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new InvoiceListItemDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                InvoiceNumber = x.InvoiceNumber,
                Amount = x.Amount,
                Date = x.Date,
                Description = x.Description,
                CreatedAt = x.CreatedAt,
                IsVoided = _dbContext.LedgerEntries.Any(l =>
                    l.OrganizationId == organizationId &&
                    l.InvoiceNumber == x.InvoiceNumber &&
                    l.Type == LedgerEntryType.Invoice &&
                    l.IsVoided)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<InvoiceCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var data = await _dbContext.Projects
            .AsNoTracking()
            .Where(x => x.Id == projectId && x.OrganizationId == organizationId)
            .Select(x => new
            {
                x.Id,
                x.Status,
                ClientStatus = x.Client!.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
        {
            throw new NotFoundException("Project not found.");
        }

        if (data.Status == ProjectStatus.Archived)
        {
            throw new InvalidOperationException("Archived projects cannot receive invoices.");
        }

        if (data.ClientStatus == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot receive invoices.");
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

        var data = await _dbContext.Projects
            .Where(x => x.Id == dto.ProjectId && x.OrganizationId == organizationId)
            .Select(x => new
            {
                Project = x,
                x.Status,
                ClientStatus = x.Client!.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
        {
            throw new NotFoundException("Project not found.");
        }

        if (data.Status == ProjectStatus.Archived)
        {
            throw new InvalidOperationException("Archived projects cannot receive invoices.");
        }

        if (data.ClientStatus == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot receive invoices.");
        }

        var project = data.Project;

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
            PaymentReference = null,
            DiscountReference = null,
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

    public async Task VoidAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(
                x => x.Id == invoiceId && x.OrganizationId == organizationId,
                cancellationToken);

        if (invoice is null)
        {
            throw new NotFoundException("Invoice not found.");
        }

        var ledger = await _dbContext.LedgerEntries
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId &&
                     x.InvoiceNumber == invoice.InvoiceNumber &&
                     x.Type == LedgerEntryType.Invoice,
                cancellationToken);

        if (ledger is null)
        {
            throw new InvalidOperationException("Ledger entry not found.");
        }

        if (ledger.IsVoided)
        {
            throw new InvalidOperationException("Invoice already voided.");
        }

        var now = DateTime.UtcNow;
        var userId = GetRequiredUserId();

        var reversal = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = ledger.OrganizationId,
            ClientId = ledger.ClientId,
            ProjectId = ledger.ProjectId,
            Type = LedgerEntryType.Invoice,
            Amount = -ledger.Amount,
            OccurredAt = now,
            Notes = $"Void of invoice {ledger.InvoiceNumber}",
            InvoiceNumber = null,
            PaymentReference = null,
            DiscountReference = null,
            IsVoided = false,
            VoidedAt = null,
            CreatedAt = now,
            CreatedBy = userId
        };

        ledger.IsVoided = true;
        ledger.VoidedAt = now;

        _dbContext.LedgerEntries.Add(reversal);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string GetRequiredUserId()
    {
        return _currentUser.UserId
            ?? throw new InvalidOperationException(
                "Current user is not authenticated.");
    }
}