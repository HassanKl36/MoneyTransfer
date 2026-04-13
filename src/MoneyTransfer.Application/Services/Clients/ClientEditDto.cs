using MoneyTransfer.Domain.Enums;
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

    public ClientStatus Status { get; set; } = ClientStatus.Active;

    public bool CreatePortalAccount { get; set; }

    [StringLength(256)]
    public string? PortalUsername { get; set; }

    [DataType(DataType.Password)]
    public string? PortalPassword { get; set; }

    [DataType(DataType.Password)]
    [Compare(nameof(PortalPassword), ErrorMessage = "Password and confirmation password do not match.")]
    public string? ConfirmPortalPassword { get; set; }

    public bool HasPortalAccount { get; set; }
}