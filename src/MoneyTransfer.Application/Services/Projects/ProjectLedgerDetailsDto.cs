using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Application.Services.Projects;

public sealed class ProjectLedgerDetailsDto
{
    public decimal TotalInvoiced { get; set; }

    public decimal TotalPaid { get; set; }

    public decimal TotalDiscounted { get; set; }

    public decimal RemainingBalance { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public LedgerEntryType? TransactionType { get; set; }

    public IReadOnlyList<ProjectLedgerEntryDto> Entries { get; set; } = Array.Empty<ProjectLedgerEntryDto>();
}