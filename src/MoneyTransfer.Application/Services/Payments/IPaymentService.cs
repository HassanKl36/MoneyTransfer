namespace MoneyTransfer.Application.Services.Payments;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentListItemDto>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<PaymentCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        PaymentCreateDto dto,
        CancellationToken cancellationToken = default);
}