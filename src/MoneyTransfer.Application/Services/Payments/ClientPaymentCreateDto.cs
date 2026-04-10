using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Application.Services.Payments;

public sealed class ClientPaymentCreateDto
{
    [Required]
    public Guid ClientId { get; set; }

    public string ClientName { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal TotalAmount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [StringLength(100)]
    public string? PaymentMethod { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public IReadOnlyList<ClientPaymentProjectOptionDto> AvailableProjects { get; set; }
        = Array.Empty<ClientPaymentProjectOptionDto>();

    [MinLength(1)]
    public List<ClientPaymentAllocationLineDto> Allocations { get; set; }
        = new();
}