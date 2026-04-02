using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Domain.Entities;

public sealed class Project
{
    public Guid Id { get; set; }

    // For scoping simplicity (required)
    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string? Description { get; set; }

    public ProjectStatus Status { get; set; }

    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}