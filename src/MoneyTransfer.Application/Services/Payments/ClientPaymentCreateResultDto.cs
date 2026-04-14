namespace MoneyTransfer.Application.Services.Payments;

public sealed class ClientPaymentCreateResultDto
{
    public Guid HeaderId { get; set; }

    public string PaymentReference { get; set; } = string.Empty;

    public IReadOnlyList<Guid> PaymentIds { get; set; } = Array.Empty<Guid>();
}