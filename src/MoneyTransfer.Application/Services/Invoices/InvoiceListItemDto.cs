namespace MoneyTransfer.Application.Services.Invoices;

public sealed class InvoiceListItemDto
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}