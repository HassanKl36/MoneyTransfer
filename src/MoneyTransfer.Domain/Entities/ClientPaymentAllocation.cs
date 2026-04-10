namespace MoneyTransfer.Domain.Entities;

public sealed class ClientPaymentAllocation
{
    public Guid Id { get; set; }

    public Guid ClientPaymentHeaderId { get; set; }
    public ClientPaymentHeader? ClientPaymentHeader { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public decimal Amount { get; set; }
}