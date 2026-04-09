using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Application.Services.Projects;

public sealed class ProjectLedgerEntryDto
{
    public DateTime OccurredAt { get; set; }

    public LedgerEntryType Type { get; set; }

    public decimal Amount { get; set; }

    public string? Reference { get; set; }

    public string? Notes { get; set; }
}