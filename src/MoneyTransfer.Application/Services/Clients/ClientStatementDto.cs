using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientStatementDto
{
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public DateTime AsOfDate { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public Guid? ProjectId { get; set; }

    public LedgerEntryType? TransactionType { get; set; }

    public ClientStatementSummaryDto Summary { get; set; } = new();

    public IReadOnlyList<ClientStatementEntryDto> Entries { get; set; }
        = Array.Empty<ClientStatementEntryDto>();
}