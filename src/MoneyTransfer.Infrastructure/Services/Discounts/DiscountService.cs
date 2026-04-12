using Microsoft.EntityFrameworkCore;
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
        var orgId = GetRequiredOrganizationId();

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
            .Select(x => new DiscountListItemDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                DiscountReference = x.DiscountReference,
                Amount = x.Amount,
                Date = x.Date,
                Reason = x.Reason,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<DiscountCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var orgId = GetRequiredOrganizationId();

        var exists = await _dbContext.Projects
            .AnyAsync(x => x.Id == projectId && x.OrganizationId == orgId, cancellationToken);

        if (!exists)
        {
            throw new InvalidOperationException("Project not found.");
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
        var orgId = GetRequiredOrganizationId();

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(x => x.Id == dto.ProjectId && x.OrganizationId == orgId, cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

        var currentBalance = await _dbContext.LedgerEntries
            .Where(x => x.ProjectId == dto.ProjectId && x.OrganizationId == orgId && !x.IsVoided)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var resultingBalance = currentBalance - dto.Amount;

        if (resultingBalance < 0)
        {
            throw new InvalidOperationException("Discount would result in negative project balance.");
        }

        var reference = await _identityGenerator.GenerateDiscountReferenceAsync(orgId, cancellationToken);

        var now = DateTime.UtcNow;
        var userId = GetRequiredUserId();
        var reason = dto.Reason.Trim();

        var discount = new Discount
        {
            Id = Guid.NewGuid(),
            ProjectId = dto.ProjectId,
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
            Notes = $"Discount Ref: {reference} | {reason}",
            InvoiceNumber = null,
            IsVoided = false,
            CreatedAt = now,
            CreatedBy = userId
        };

        _dbContext.Discounts.Add(discount);
        _dbContext.LedgerEntries.Add(ledger);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid GetRequiredOrganizationId()
    {
        return _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException("No organization.");
    }

    private string GetRequiredUserId()
    {
        return _currentUser.UserId
            ?? throw new InvalidOperationException("No user.");
    }
}