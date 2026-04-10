namespace MoneyTransfer.Application.Services.Payments;

public sealed class ClientPaymentProjectOptionDto
{
    public Guid ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string ProjectCode { get; set; } = string.Empty;

    public decimal CurrentBalance { get; set; }
}