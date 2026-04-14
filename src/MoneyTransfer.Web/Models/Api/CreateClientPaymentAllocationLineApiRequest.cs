using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Web.Models.Api;

public sealed class CreateClientPaymentAllocationLineApiRequest
{
    [Required]
    public Guid ProjectId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }
}