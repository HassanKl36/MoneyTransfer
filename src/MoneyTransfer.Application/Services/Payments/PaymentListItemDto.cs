namespace MoneyTransfer.Application.Services.Payments;

public sealed class PaymentListItemDto
{
    public Guid Id { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}