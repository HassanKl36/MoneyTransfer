using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Domain.Entities;

public sealed class LedgerEntry
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public LedgerEntryType Type { get; set; }

    /// <summary>
    /// Signed amount: Invoice +, Payment/Discount -, Adjustment +/-.
    /// Stored as decimal(18,2) later via EF config.
    /// </summary>
    public decimal Amount { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Optional reason/notes.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Nullable; only meaningful for Invoice entries later.
    /// </summary>
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Nullable; only meaningful for Payment entries.
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Nullable; only meaningful for Discount entries.
    /// </summary>
    public string? DiscountReference { get; set; }

    public bool IsVoided { get; set; }
    public DateTime? VoidedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public string CreatedBy { get; set; } = string.Empty;
}