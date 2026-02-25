namespace MoneyTransfer.Domain.Entities;

public sealed class Client
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}