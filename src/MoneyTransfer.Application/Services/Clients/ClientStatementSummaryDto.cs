namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientStatementSummaryDto
{
    public decimal OpeningBalance { get; set; }

    public decimal TotalInvoices { get; set; }

    public decimal TotalPayments { get; set; }

    public decimal TotalDiscounts { get; set; }

    public decimal NetChange { get; set; }

    public decimal ClosingBalance { get; set; }
}