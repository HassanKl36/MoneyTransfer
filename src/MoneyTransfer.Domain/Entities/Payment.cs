namespace MoneyTransfer.Domain.Entities;

public sealed class Payment
{
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public decimal Amount { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}