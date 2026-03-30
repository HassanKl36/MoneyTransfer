using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.FinancialIdentity;

public sealed class FinancialIdentityGenerator : IFinancialIdentityGenerator
{
    private readonly MoneyTransferDbContext _dbContext;

    public FinancialIdentityGenerator(MoneyTransferDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<string> GenerateProjectCodeAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync(
            organizationId,
            prefix: "PRJ",
            padding: 4,
            selector: SequenceType.Project,
            cancellationToken: cancellationToken);
    }

    public Task<string> GenerateInvoiceNumberAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync(
            organizationId,
            prefix: "INV",
            padding: 6,
            selector: SequenceType.Invoice,
            cancellationToken: cancellationToken);
    }

    public Task<string> GeneratePaymentReferenceAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return GenerateAsync(
            organizationId,
            prefix: "PAY",
            padding: 6,
            selector: SequenceType.Payment,
            cancellationToken: cancellationToken);
    }

    private async Task<string> GenerateAsync(
        Guid organizationId,
        string prefix,
        int padding,
        SequenceType selector,
        CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

            var sequence = await _dbContext.OrganizationSequences
                .SingleOrDefaultAsync(x => x.OrganizationId == organizationId, cancellationToken);

            if (sequence is null)
            {
                var organizationExists = await _dbContext.Organizations
                    .AsNoTracking()
                    .AnyAsync(x => x.Id == organizationId, cancellationToken);

                if (!organizationExists)
                {
                    throw new InvalidOperationException("Organization not found for identity generation.");
                }

                sequence = new OrganizationSequence
                {
                    OrganizationId = organizationId,
                    NextProjectNumber = 1,
                    NextInvoiceNumber = 1,
                    NextPaymentNumber = 1
                };

                _dbContext.OrganizationSequences.Add(sequence);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            var nextValue = selector switch
            {
                SequenceType.Project => sequence.NextProjectNumber++,
                SequenceType.Invoice => sequence.NextInvoiceNumber++,
                SequenceType.Payment => sequence.NextPaymentNumber++,
                _ => throw new InvalidOperationException("Unsupported sequence type.")
            };

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return $"{prefix}-{nextValue.ToString($"D{padding}")}";
        });
    }

    private enum SequenceType
    {
        Project = 1,
        Invoice = 2,
        Payment = 3
    }
}