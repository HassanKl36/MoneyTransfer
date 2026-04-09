using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Projects;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.Projects;

public sealed class ProjectFinancialService : IProjectFinancialService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;

    public ProjectFinancialService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
    }

    public async Task<ProjectFinancialSummaryDto?> GetSummaryAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var projectExists = await _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == projectId
                  && p.OrganizationId == organizationId,
                cancellationToken);

        if (!projectExists)
        {
            return null;
        }

        var ledgerEntries = _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.ProjectId == projectId &&
                x.OrganizationId == organizationId &&
                !x.IsVoided);

        var totalInvoiced = await ledgerEntries
            .Where(x => x.Type == MoneyTransfer.Domain.Enums.LedgerEntryType.Invoice)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var totalPaid = await ledgerEntries
            .Where(x => x.Type == MoneyTransfer.Domain.Enums.LedgerEntryType.Payment)
            .SumAsync(x => (decimal?)(-x.Amount), cancellationToken) ?? 0m;

        var balance = await ledgerEntries
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        return new ProjectFinancialSummaryDto
        {
            TotalInvoiced = totalInvoiced,
            TotalPaid = totalPaid,
            RemainingBalance = balance
        };
    }

    private Guid GetRequiredOrganizationId()
    {
        return _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException(
                "Current user is not associated with an organization.");
    }
}