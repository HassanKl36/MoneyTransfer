namespace MoneyTransfer.Domain.Entities;

public sealed class ClientPaymentHeader
{
    public Guid Id { get; set; }

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string PaymentReference { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTime Date { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<ClientPaymentAllocation> Allocations { get; set; }
        = new List<ClientPaymentAllocation>();
}