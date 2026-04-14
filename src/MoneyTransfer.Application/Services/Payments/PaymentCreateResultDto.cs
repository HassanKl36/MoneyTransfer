namespace MoneyTransfer.Application.Services.Payments;

public sealed class PaymentCreateResultDto
{
    public Guid Id { get; set; }

    public string PaymentReference { get; set; } = string.Empty;
}