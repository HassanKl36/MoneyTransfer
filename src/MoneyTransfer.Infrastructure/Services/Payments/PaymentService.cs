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
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

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
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

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

    public async Task<PaymentCreateResultDto> CreateAsync(
        PaymentCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

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
        var paymentMethod = NormalizeOptionalText(dto.PaymentMethod);
        var description = NormalizeOptionalText(dto.Description);

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

        return new PaymentCreateResultDto
        {
            Id = payment.Id,
            PaymentReference = payment.PaymentReference
        };
    }

    public async Task<ClientPaymentCreateDto> InitializeClientAllocationCreateAsync(
        Guid clientId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var client = await _dbContext.Clients
            .AsNoTracking()
            .Where(c => c.Id == clientId && c.OrganizationId == organizationId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (client is null)
        {
            throw new InvalidOperationException("Client not found.");
        }

        if (client.Status == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot receive payments.");
        }

        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(p =>
                p.OrganizationId == organizationId &&
                p.ClientId == clientId &&
                p.Status != ProjectStatus.Archived)
            .OrderBy(p => p.Name)
            .Select(p => new ClientPaymentProjectOptionDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                ProjectCode = p.Code,
                CurrentBalance = 0m
            })
            .ToListAsync(cancellationToken);

        var balances = await _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.OrganizationId == organizationId &&
                x.ClientId == clientId &&
                !x.IsVoided)
            .GroupBy(x => x.ProjectId)
            .Select(g => new
            {
                ProjectId = g.Key,
                Balance = g.Sum(x => x.Amount)
            })
            .ToDictionaryAsync(
                x => x.ProjectId,
                x => x.Balance,
                cancellationToken);

        foreach (var project in projects)
        {
            if (balances.TryGetValue(project.ProjectId, out var balance))
            {
                project.CurrentBalance = balance;
            }
        }

        return new ClientPaymentCreateDto
        {
            ClientId = client.Id,
            ClientName = client.Name,
            Date = DateTime.Today,
            AvailableProjects = projects,
            Allocations = projects
                .Select(p => new ClientPaymentAllocationLineDto
                {
                    ProjectId = p.ProjectId,
                    Amount = 0m
                })
                .ToList()
        };
    }

    public async Task<ClientPaymentCreateResultDto> CreateClientAllocationAsync(
        ClientPaymentCreateDto dto,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);
        var userId = GetRequiredUserId();

        var paymentMethod = NormalizeOptionalText(dto.PaymentMethod);
        var description = NormalizeOptionalText(dto.Description);

        var allocations = (dto.Allocations ?? new List<ClientPaymentAllocationLineDto>())
            .Where(x => x is not null && x.Amount > 0m)
            .Select(x => new ClientPaymentAllocationLineDto
            {
                ProjectId = x.ProjectId,
                Amount = x.Amount
            })
            .ToList();

        if (allocations.Count == 0)
        {
            throw new InvalidOperationException("At least one allocation is required.");
        }

        if (allocations.Any(x => x.ProjectId == Guid.Empty))
        {
            throw new InvalidOperationException("Each allocation must include a valid project.");
        }

        if (allocations.Any(x => x.Amount <= 0m))
        {
            throw new InvalidOperationException("Allocation amounts must be greater than zero.");
        }

        var duplicatedProjectIds = allocations
            .GroupBy(x => x.ProjectId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedProjectIds.Count > 0)
        {
            throw new InvalidOperationException("Each project may only appear once in the allocation list.");
        }

        var totalAllocated = allocations.Sum(x => x.Amount);

        if (totalAllocated != dto.TotalAmount)
        {
            throw new InvalidOperationException("Total allocations must exactly equal the total payment amount.");
        }

        var client = await _dbContext.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Id == dto.ClientId && c.OrganizationId == organizationId,
                cancellationToken);

        if (client is null)
        {
            throw new InvalidOperationException("Client not found.");
        }

        if (client.Status == ClientStatus.Archived)
        {
            throw new InvalidOperationException("Archived clients cannot receive payments.");
        }

        var projectIds = allocations
            .Select(x => x.ProjectId)
            .ToList();

        var projects = await _dbContext.Projects
            .AsNoTracking()
            .Where(p =>
                p.OrganizationId == organizationId &&
                projectIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (projects.Count != projectIds.Count)
        {
            throw new InvalidOperationException("One or more selected projects were not found.");
        }

        if (projects.Any(p => p.ClientId != dto.ClientId))
        {
            throw new InvalidOperationException("All allocated projects must belong to the selected client.");
        }

        if (projects.Any(p => p.Status == ProjectStatus.Archived))
        {
            throw new InvalidOperationException("Archived projects cannot receive payments.");
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var paymentReference = await _financialIdentityGenerator.GeneratePaymentReferenceAsync(
            organizationId,
            cancellationToken);

        var now = DateTime.UtcNow;

        var header = new ClientPaymentHeader
        {
            Id = Guid.NewGuid(),
            ClientId = dto.ClientId,
            OrganizationId = organizationId,
            PaymentReference = paymentReference,
            TotalAmount = dto.TotalAmount,
            Date = dto.Date,
            PaymentMethod = paymentMethod,
            Description = description,
            CreatedAt = now,
            CreatedBy = userId
        };

        var allocationsByProjectId = allocations.ToDictionary(x => x.ProjectId, x => x.Amount);

        var orderedProjects = projects
            .OrderBy(p => p.Name)
            .ToList();

        var allocationEntities = orderedProjects
            .Select(p => new ClientPaymentAllocation
            {
                Id = Guid.NewGuid(),
                ClientPaymentHeaderId = header.Id,
                ProjectId = p.Id,
                Amount = allocationsByProjectId[p.Id]
            })
            .ToList();

        var paymentEntities = orderedProjects
            .Select(p => new Payment
            {
                Id = Guid.NewGuid(),
                ProjectId = p.Id,
                OrganizationId = organizationId,
                PaymentReference = paymentReference,
                Amount = allocationsByProjectId[p.Id],
                Date = dto.Date,
                PaymentMethod = paymentMethod,
                Description = description,
                CreatedAt = now,
                CreatedBy = userId
            })
            .ToList();

        var ledgerEntries = orderedProjects
            .Select(p => new LedgerEntry
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                ClientId = dto.ClientId,
                ProjectId = p.Id,
                Type = LedgerEntryType.Payment,
                Amount = -allocationsByProjectId[p.Id],
                OccurredAt = dto.Date,
                Notes = BuildLedgerNotes(paymentReference, paymentMethod, description),
                InvoiceNumber = null,
                IsVoided = false,
                VoidedAt = null,
                CreatedAt = now,
                CreatedBy = userId
            })
            .ToList();

        _dbContext.Set<ClientPaymentHeader>().Add(header);
        _dbContext.Set<ClientPaymentAllocation>().AddRange(allocationEntities);
        _dbContext.Set<Payment>().AddRange(paymentEntities);
        _dbContext.LedgerEntries.AddRange(ledgerEntries);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ClientPaymentCreateResultDto
        {
            HeaderId = header.Id,
            PaymentReference = header.PaymentReference,
            PaymentIds = paymentEntities
                .Select(x => x.Id)
                .ToList()
        };
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
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
}