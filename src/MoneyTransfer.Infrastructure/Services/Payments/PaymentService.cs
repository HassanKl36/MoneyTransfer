using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Payments;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;

    public PaymentService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
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

        if (project is null || project.IsArchived)
        {
            throw new InvalidOperationException("Project not available.");
        }

        return await _dbContext.Set<Payment>()
            .AsNoTracking()
            .Where(p => p.ProjectId == projectId && p.OrganizationId == organizationId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PaymentListItemDto
            {
                Id = p.Id,
                Amount = p.Amount,
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

        if (project is null || project.IsArchived)
        {
            throw new InvalidOperationException("Project not available for payment creation.");
        }

        return new PaymentCreateDto
        {
            ProjectId = projectId
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

        if (project.IsArchived)
        {
            throw new InvalidOperationException("Archived projects cannot receive payments.");
        }

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            OrganizationId = organizationId,
            Amount = dto.Amount,
            Description = string.IsNullOrWhiteSpace(dto.Description)
                ? null
                : dto.Description.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Set<Payment>().Add(payment);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Guid GetRequiredOrganizationId()
    {
        return _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException(
                "Current user is not associated with an organization.");
    }
}