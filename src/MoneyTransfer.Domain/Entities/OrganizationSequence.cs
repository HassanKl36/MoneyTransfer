namespace MoneyTransfer.Domain.Entities;

public sealed class OrganizationSequence
{
    public Guid OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public int NextProjectNumber { get; set; }

    public int NextInvoiceNumber { get; set; }

    public int NextPaymentNumber { get; set; }

    public int NextDiscountNumber { get; set; }
}