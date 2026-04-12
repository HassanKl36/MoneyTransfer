namespace MoneyTransfer.Domain.Entities;

public sealed class Discount
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string DiscountReference { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime Date { get; set; }

    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}