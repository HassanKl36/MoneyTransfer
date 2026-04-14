namespace MoneyTransfer.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public string KeyPrefix { get; set; } = string.Empty;

    public string HashedKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public Organization? Organization { get; set; }
}