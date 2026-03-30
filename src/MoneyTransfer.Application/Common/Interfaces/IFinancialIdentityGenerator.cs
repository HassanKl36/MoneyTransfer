namespace MoneyTransfer.Application.Common.Interfaces;

public interface IFinancialIdentityGenerator
{
    Task<string> GenerateProjectCodeAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<string> GenerateInvoiceNumberAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);

    Task<string> GeneratePaymentReferenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}