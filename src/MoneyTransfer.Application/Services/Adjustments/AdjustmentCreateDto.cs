using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Application.Services.Adjustments;

public sealed class AdjustmentCreateDto : IValidatableObject
{
    [Required]
    public Guid ProjectId { get; set; }

    [Range(typeof(decimal), "-999999999999.99", "999999999999.99")]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Amount == 0m)
        {
            yield return new ValidationResult(
                "Amount cannot be zero.",
                new[] { nameof(Amount) });
        }
    }
}