using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Web.Models.Api;

public sealed class CreateClientPaymentAllocationApiRequest
{
    [Required]
    public Guid ClientId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal TotalAmount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [StringLength(100)]
    public string? PaymentMethod { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [MinLength(1)]
    public List<CreateClientPaymentAllocationLineApiRequest> Allocations { get; set; } = new();
}