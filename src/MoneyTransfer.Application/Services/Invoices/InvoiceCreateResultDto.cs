namespace MoneyTransfer.Application.Services.Invoices;

public sealed class InvoiceCreateResultDto
{
    public Guid Id { get; set; }

    public string InvoiceNumber { get; set; } = string.Empty;
}