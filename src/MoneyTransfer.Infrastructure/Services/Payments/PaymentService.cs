using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Payments;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Domain.Enums;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly ICurrentUser _currentUser;
    private readonly IFinancialIdentityGenerator _financialIdentityGenerator;

    public PaymentService(
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

    public async Task<IReadOnlyList<PaymentListItemDto>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == projectId && p.OrganizationId == organizationId,
                cancellationToken);

        if (project is null || project.Status == ProjectStatus.Archived)
        {
            throw new InvalidOperationException("Project not available.");
        }

        return await _dbContext.Set<Payment>()
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId && p.OrganizationId == organizationId)
            .OrderByDescending(p => p.Date)
            .Select(p => new PaymentListItemDto
            {
                Id = p.Id,
                PaymentReference = p.PaymentReference,
                Amount = p.Amount,
                Date = p.Date,
                PaymentMethod = p.PaymentMethod,
                Description = p.Description,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<PaymentCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var project = await _dbContext.Projects
            .AsNoTracking()
            .FirstOrDefaultAsync(
                p => p.Id == projectId && p.OrganizationId == organizationId,
                cancellationToken);

        if (project is null || project.Status == ProjectStatus.Archived)
        {
            throw new InvalidOperationException("Project not available for payment creation.");
        }

        return new PaymentCreateDto
        {
            ProjectId = projectId,
            Date = DateTime.Today
        };
    }

    public async Task CreateAsync(
        PaymentCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var project = await _dbContext.Projects
            .FirstOrDefaultAsync(
                p => p.Id == dto.ProjectId && p.OrganizationId == organizationId,
                cancellationToken);

        if (project is null)
        {
            throw new InvalidOperationException("Project not found.");
        }

        if (project.Status == ProjectStatus.Archived)
        {
            throw new InvalidOperationException("Archived projects cannot receive payments.");
        }

        var paymentReference = await _financialIdentityGenerator.GeneratePaymentReferenceAsync(
            organizationId,
            cancellationToken);

        var now = DateTime.UtcNow;
        var userId = GetRequiredUserId();
        var paymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod)
            ? null
            : dto.PaymentMethod.Trim();
        var description = string.IsNullOrWhiteSpace(dto.Description)
            ? null
            : dto.Description.Trim();

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            OrganizationId = organizationId,
            PaymentReference = paymentReference,
            Amount = dto.Amount,
            Date = dto.Date,
            PaymentMethod = paymentMethod,
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
            Type = LedgerEntryType.Payment,
            Amount = -dto.Amount,
            OccurredAt = dto.Date,
            Notes = BuildLedgerNotes(paymentReference, paymentMethod, description),
            InvoiceNumber = null,
            IsVoided = false,
            VoidedAt = null,
            CreatedAt = now,
            CreatedBy = userId
        };

        _dbContext.Set<Payment>().Add(payment);
        _dbContext.LedgerEntries.Add(ledgerEntry);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string BuildLedgerNotes(
        string paymentReference,
        string? paymentMethod,
        string? description)
    {
        var parts = new List<string>
        {
            $"Payment Ref: {paymentReference}"
        };

        if (!string.IsNullOrWhiteSpace(paymentMethod))
        {
            parts.Add($"Method: {paymentMethod}");
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            parts.Add(description);
        }

        return string.Join(" | ", parts);
    }

    private string GetRequiredUserId()
    {
        return _currentUser.UserId
            ?? throw new InvalidOperationException(
                "Current user is not authenticated.");
    }

    private Guid GetRequiredOrganizationId()
    {
        return _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException(
                "Current user is not associated with an organization.");
    }
}