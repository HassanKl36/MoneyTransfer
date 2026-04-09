using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Application.Services.Projects;

public interface IProjectFinancialService
{
    Task<ProjectLedgerDetailsDto?> GetLedgerDetailsAsync(
        Guid projectId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        LedgerEntryType? transactionType = null,
        CancellationToken cancellationToken = default);
}