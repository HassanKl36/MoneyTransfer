namespace MoneyTransfer.Application.Services.Invoices;

public interface IInvoiceService
{
    Task<IReadOnlyList<InvoiceListItemDto>> GetByProjectAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<InvoiceCreateDto> InitializeCreateAsync(
        Guid projectId,
        CancellationToken cancellationToken = default);

    Task<InvoiceCreateResultDto> CreateAsync(
        InvoiceCreateDto dto,
        CancellationToken cancellationToken = default);

    Task VoidAsync(
        Guid invoiceId, 
        CancellationToken cancellationToken = default);

}