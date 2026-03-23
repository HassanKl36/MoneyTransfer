namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = null!;
    public string? Email { get; set; }
    public bool IsArchived { get; set; }
}