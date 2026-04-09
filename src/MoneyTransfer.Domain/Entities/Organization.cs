namespace MoneyTransfer.Domain.Entities;

public sealed class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;

    public OrganizationSequence? Sequence { get; set; }
    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}