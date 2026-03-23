using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Application.Services.Clients;

public sealed class ClientEditDto
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsArchived { get; set; }
}