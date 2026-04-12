namespace MoneyTransfer.Application.Services.Discounts;

public interface IDiscountService
{
    Task<IReadOnlyList<DiscountListItemDto>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<DiscountCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        DiscountCreateDto dto,
        CancellationToken cancellationToken = default);
}