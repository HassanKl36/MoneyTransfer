using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Application.Services.Discounts;

public sealed class DiscountCreateDto
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(1000)]
    public string Reason { get; set; } = string.Empty;
}