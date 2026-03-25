namespace MoneyTransfer.Application.Services.Invoices;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceListItemDto>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task<InvoiceCreateDto> InitializeCreateAsync(Guid projectId, CancellationToken cancellationToken = default);

    Task CreateAsync(InvoiceCreateDto dto, CancellationToken cancellationToken = default);
}