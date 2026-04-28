using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Exceptions;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Adjustments;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Domain.Enums;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Adjustments;

public sealed class AdjustmentService : IAdjustmentService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;

    public AdjustmentService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization,
        ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
        _currentUser = currentUser;
    }

    public async Task<AdjustmentCreateDto> InitializeCreateAsync(
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
            throw new InvalidOperationException("Archived projects cannot receive adjustments.");
        }

        if (data.ClientStatus == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot receive adjustments.");
        }

        return new AdjustmentCreateDto
        {
            ProjectId = projectId,
            Date = DateTime.Today
        };
    }

    public async Task CreateAsync(
        AdjustmentCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        if (dto.Amount == 0m)
        {
            throw new InvalidOperationException("Amount cannot be zero.");
        }

        if (string.IsNullOrWhiteSpace(dto.Reason))
        {
            throw new InvalidOperationException("Reason is required.");
        }

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
            throw new InvalidOperationException("Archived projects cannot receive adjustments.");
        }

        if (data.ClientStatus == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot receive adjustments.");
        }

        var project = data.Project;
        var now = DateTime.UtcNow;
        var userId = GetRequiredUserId();
        var reason = dto.Reason.Trim();

        var ledgerEntry = new LedgerEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ClientId = project.ClientId,
            ProjectId = project.Id,
            Type = LedgerEntryType.Adjustment,
            Amount = dto.Amount,
            OccurredAt = dto.Date,
            Notes = reason,
            InvoiceNumber = null,
            PaymentReference = null,
            DiscountReference = null,
            IsVoided = false,
            VoidedAt = null,
            CreatedAt = now,
            CreatedBy = userId
        };

        _dbContext.LedgerEntries.Add(ledgerEntry);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private string GetRequiredUserId()
    {
        return _currentUser.UserId
            ?? throw new InvalidOperationException("Current user is not authenticated.");
    }
}