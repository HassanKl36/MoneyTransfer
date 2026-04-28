namespace MoneyTransfer.Application.Services.Adjustments;

public interface IAdjustmentService
{
    Task<AdjustmentCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        AdjustmentCreateDto dto,
        CancellationToken cancellationToken = default);
}