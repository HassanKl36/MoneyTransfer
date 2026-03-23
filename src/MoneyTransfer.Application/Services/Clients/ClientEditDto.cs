using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientEditDto
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = null!;

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    public bool IsArchived { get; set; }
}