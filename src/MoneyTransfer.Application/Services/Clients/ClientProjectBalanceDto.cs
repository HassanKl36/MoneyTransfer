namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientProjectBalanceDto
{
    public Guid ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string ProjectCode { get; set; } = string.Empty;

    public decimal Balance { get; set; }
}