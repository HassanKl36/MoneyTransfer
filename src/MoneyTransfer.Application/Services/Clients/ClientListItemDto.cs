namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
}