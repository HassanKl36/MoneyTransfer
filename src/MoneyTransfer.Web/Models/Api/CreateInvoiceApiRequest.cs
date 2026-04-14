using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Web.Models.Api;

public sealed class CreateInvoiceApiRequest
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }
}