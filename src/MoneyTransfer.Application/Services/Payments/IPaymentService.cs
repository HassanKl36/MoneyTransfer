namespace MoneyTransfer.Application.Services.Payments;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentListItemDto>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<PaymentCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<PaymentCreateResultDto> CreateAsync(
        PaymentCreateDto dto,
        CancellationToken cancellationToken = default);

    Task<ClientPaymentCreateDto> InitializeClientAllocationCreateAsync(
        Guid clientId,
        CancellationToken cancellationToken = default);

    Task<ClientPaymentCreateResultDto> CreateClientAllocationAsync(
        ClientPaymentCreateDto dto,
        CancellationToken cancellationToken = default);

    Task VoidAsync(
        Guid paymentId, 
        CancellationToken cancellationToken = default);
}