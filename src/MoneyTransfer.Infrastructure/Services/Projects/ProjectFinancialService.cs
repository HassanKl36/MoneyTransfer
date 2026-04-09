using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Projects;
using MoneyTransfer.Domain.Enums;
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

    public async Task<ProjectLedgerDetailsDto?> GetLedgerDetailsAsync(
        Guid projectId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        LedgerEntryType? transactionType = null,
        CancellationToken cancellationToken = default)
    {
        var organizationId = GetRequiredOrganizationId();

        var projectExists = await _dbContext.Projects
            .AsNoTracking()
            .AnyAsync(
                p => p.Id == projectId &&
                     p.OrganizationId == organizationId,
                cancellationToken);

        if (!projectExists)
        {
            return null;
        }

        var allLedgerEntriesQuery = _dbContext.LedgerEntries
            .AsNoTracking()
            .Where(x =>
                x.ProjectId == projectId &&
                x.OrganizationId == organizationId &&
                !x.IsVoided);

        var totalInvoiced = await allLedgerEntriesQuery
            .Where(x => x.Type == LedgerEntryType.Invoice)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var totalPaid = await allLedgerEntriesQuery
            .Where(x => x.Type == LedgerEntryType.Payment)
            .SumAsync(x => (decimal?)(-x.Amount), cancellationToken) ?? 0m;

        var remainingBalance = await allLedgerEntriesQuery
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var filteredLedgerEntriesQuery = allLedgerEntriesQuery;

        if (fromDate.HasValue)
        {
            var fromDateValue = fromDate.Value.Date;
            filteredLedgerEntriesQuery = filteredLedgerEntriesQuery
                .Where(x => x.OccurredAt >= fromDateValue);
        }

        if (toDate.HasValue)
        {
            var toDateExclusive = toDate.Value.Date.AddDays(1);
            filteredLedgerEntriesQuery = filteredLedgerEntriesQuery
                .Where(x => x.OccurredAt < toDateExclusive);
        }

        if (transactionType.HasValue)
        {
            filteredLedgerEntriesQuery = filteredLedgerEntriesQuery
                .Where(x => x.Type == transactionType.Value);
        }

        var entries = await filteredLedgerEntriesQuery
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.CreatedAt)
            .Select(x => new ProjectLedgerEntryDto
            {
                OccurredAt = x.OccurredAt,
                Type = x.Type,
                Amount = x.Amount,
                Reference = x.Type == LedgerEntryType.Invoice
                    ? x.InvoiceNumber
                    : x.Type == LedgerEntryType.Payment
                        ? GetPaymentReference(x.Notes)
                        : null,
                Notes = x.Type == LedgerEntryType.Payment
                    ? GetPaymentNotes(x.Notes)
                    : x.Notes
            })
            .ToListAsync(cancellationToken);

        return new ProjectLedgerDetailsDto
        {
            TotalInvoiced = totalInvoiced,
            TotalPaid = totalPaid,
            RemainingBalance = remainingBalance,
            FromDate = fromDate,
            ToDate = toDate,
            TransactionType = transactionType,
            Entries = entries
        };
    }

    private static string? GetPaymentReference(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var firstSegment = notes.Split('|', 2)[0].Trim();

        const string prefix = "Payment Ref:";

        if (firstSegment.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return firstSegment.Substring(prefix.Length).Trim();
        }

        return firstSegment;
    }

    private static string? GetPaymentNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var parts = notes.Split('|', 2);

        if (parts.Length < 2)
        {
            return null;
        }

        var remainingNotes = parts[1].Trim();

        return string.IsNullOrWhiteSpace(remainingNotes)
            ? null
            : remainingNotes;
    }

    private Guid GetRequiredOrganizationId()
    {
        return _currentOrganization.OrganizationId
            ?? throw new InvalidOperationException(
                "Current user is not associated with an organization.");
    }
}