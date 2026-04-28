using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientLedgerDetailsDto
{
    public string ClientName { get; set; } = string.Empty;

    public decimal TotalBalance { get; set; }

    public DateTime AsOfDate { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public Guid? ProjectId { get; set; }

    public LedgerEntryType? TransactionType { get; set; }

    public IReadOnlyList<ClientProjectBalanceDto> Projects { get; set; }
        = Array.Empty<ClientProjectBalanceDto>();

    public IReadOnlyList<ClientLedgerEntryDto> Entries { get; set; }
        = Array.Empty<ClientLedgerEntryDto>();
}