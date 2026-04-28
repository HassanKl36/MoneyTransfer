namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientDetailsDto
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty; 

    public string PhoneNumber { get; set; } = string.Empty;

    public string? Email { get; set; }

    public decimal TotalBalance { get; set; }

    public DateTime AsOfDate { get; set; }

    public IReadOnlyList<ClientProjectBalanceDto> Projects { get; set; }
        = Array.Empty<ClientProjectBalanceDto>();
}