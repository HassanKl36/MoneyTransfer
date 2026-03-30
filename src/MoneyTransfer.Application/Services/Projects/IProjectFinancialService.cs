using MoneyTransfer.Application.Services.Projects;

namespace MoneyTransfer.Application.Services.Projects;

public interface IProjectFinancialService
{
    Task<ProjectFinancialSummaryDto?> GetSummaryAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);
}