using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Exceptions;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Discounts;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Domain.Enums;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Discounts;

public sealed class DiscountService : IDiscountService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;
    private readonly IFinancialIdentityGenerator _identityGenerator;

    public DiscountService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization,
        ICurrentUser currentUser,
        IFinancialIdentityGenerator identityGenerator)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
        _identityGenerator = identityGenerator;
    }

    public async Task<IReadOnlyList<DiscountListItemDto>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var orgId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var projectExists = await _dbContext.Projects
            .AnyAsync(x => x.Id == projectId && x.OrganizationId == orgId, cancellationToken);

        if (!projectExists)
        {
            return Array.Empty<DiscountListItemDto>();
        }

        return await _dbContext.Discounts
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId && x.OrganizationId == orgId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Select(x => new DiscountListItemDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                DiscountReference = x.DiscountReference,
                Amount = x.Amount,
                Date = x.Date,
                Reason = x.Reason,
                CreatedAt = x.CreatedAt,
                IsVoided = _dbContext.LedgerEntries.Any(l =>
                    l.OrganizationId == orgId &&
                    l.DiscountReference == x.DiscountReference &&
                    l.Type == LedgerEntryType.Discount &&
                    l.IsVoided)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DiscountCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var orgId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var data = await _dbContext.Projects
            .AsNoTracking()
            .Where(x => x.Id == projectId && x.OrganizationId == orgId)
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
            throw new InvalidOperationException("Archived projects cannot receive discounts.");
        }

        if (data.ClientStatus == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot receive discounts.");
        }

        return new DiscountCreateDto
        {
            ProjectId = projectId,
            Date = DateTime.Today
        };
    }

    public async Task CreateAsync(
        DiscountCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var orgId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var data = await _dbContext.Projects
            .Where(x => x.Id == dto.ProjectId && x.OrganizationId == orgId)
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
            throw new InvalidOperationException("Archived projects cannot receive discounts.");
        }

        if (data.ClientStatus == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot receive discounts.");
        }

        var project = data.Project;

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

        var currentBalance = await _dbContext.LedgerEntries
            .Where(x => x.ProjectId == dto.ProjectId && x.OrganizationId == orgId)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var resultingBalance = currentBalance - dto.Amount;

        if (resultingBalance < 0)
        {
            throw new InvalidOperationException("Discount would result in negative project balance.");
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var reference = await _identityGenerator.GenerateDiscountReferenceAsync(orgId, cancellationToken);

            var now = DateTime.UtcNow;
            var userId = GetRequiredUserId();
            var reason = dto.Reason.Trim();

            var discount = new Discount
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                OrganizationId = orgId,
                DiscountReference = reference,
                Amount = dto.Amount,
                Date = dto.Date,
                Reason = reason,
                CreatedAt = now,
                CreatedBy = userId
            };

            var ledger = new LedgerEntry
            {
                Id = Guid.NewGuid(),
                OrganizationId = orgId,
                ClientId = project.ClientId,
                ProjectId = project.Id,
                Type = LedgerEntryType.Discount,
                Amount = -dto.Amount,
                OccurredAt = dto.Date,
                Notes = reason,
                InvoiceNumber = null,
                PaymentReference = null,
                DiscountReference = reference,
                IsVoided = false,
                VoidedAt = null,
                CreatedAt = now,
                CreatedBy = userId
            };

            _dbContext.Discounts.Add(discount);
            _dbContext.LedgerEntries.Add(ledger);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    public async Task VoidAsync(Guid discountId, CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var discount = await _dbContext.Discounts
            .FirstOrDefaultAsync(
                x => x.Id == discountId && x.OrganizationId == organizationId,
                cancellationToken);

        if (discount is null)
        {
            throw new NotFoundException("Discount not found.");
        }

        var ledger = await _dbContext.LedgerEntries
            .FirstOrDefaultAsync(
                x => x.OrganizationId == organizationId &&
                     x.DiscountReference == discount.DiscountReference &&
                     x.Type == LedgerEntryType.Discount,
                cancellationToken);

        if (ledger is null)
        {
            throw new InvalidOperationException("Ledger entry not found.");
        }

        if (ledger.IsVoided)
        {
            throw new InvalidOperationException("Discount already voided.");
        }

        var now = DateTime.UtcNow;
        var userId = GetRequiredUserId();

        var reversal = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = ledger.OrganizationId,
            ClientId = ledger.ClientId,
            ProjectId = ledger.ProjectId,
            Type = LedgerEntryType.Discount,
            Amount = -ledger.Amount,
            OccurredAt = now,
            Notes = $"Void of discount {ledger.DiscountReference}",
            InvoiceNumber = null,
            PaymentReference = null,
            DiscountReference = ledger.DiscountReference,
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
            ?? throw new InvalidOperationException("No user.");
    }
}