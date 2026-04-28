namespace MoneyTransfer.Application.Services.ApiKeys;

public sealed class ApiKeyDetailsDto
{
    public Guid Id { get; set; }

    public string KeyPrefix { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null;
}