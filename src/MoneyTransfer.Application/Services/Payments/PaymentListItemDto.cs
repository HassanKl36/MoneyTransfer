namespace MoneyTransfer.Application.Services.Payments;

public sealed class PaymentListItemDto
{
    public Guid Id { get; set; }

    public string PaymentReference { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string? PaymentMethod { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
    public bool IsVoided { get; set; }
}