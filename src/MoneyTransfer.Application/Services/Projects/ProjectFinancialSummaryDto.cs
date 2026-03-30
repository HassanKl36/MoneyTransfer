namespace MoneyTransfer.Application.Services.Projects;

public sealed class ProjectFinancialSummaryDto
{
    public decimal TotalInvoiced { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal RemainingBalance { get; set; }
}