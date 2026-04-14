using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientStatementEntryDto
{
    public Guid ProjectId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string? ProjectCode { get; set; }

    public DateTime OccurredAt { get; set; }

    public LedgerEntryType Type { get; set; }

    public decimal Amount { get; set; }

    public decimal RunningBalance { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }
}