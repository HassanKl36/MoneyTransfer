namespace MoneyTransfer.Domain.Entities;

public sealed class Organization
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Short code for invoice numbering later.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    public ICollection<Client> Clients { get; set; } = new List<Client>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<LedgerEntry> LedgerEntries { get; set; } = new List<LedgerEntry>();
}