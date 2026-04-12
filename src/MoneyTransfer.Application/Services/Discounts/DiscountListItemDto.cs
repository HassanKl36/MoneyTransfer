namespace MoneyTransfer.Application.Services.Discounts;

public sealed class DiscountListItemDto
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }

    public string DiscountReference { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}