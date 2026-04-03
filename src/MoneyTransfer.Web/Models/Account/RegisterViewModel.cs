using System.ComponentModel.DataAnnotations;

namespace MoneyTransfer.Web.Models.Account;

public sealed class RegisterViewModel
{
    [Required]
    [Display(Name = "Organization Name")]
    public string OrganizationName { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Display(Name = "Organization Code")]
    public string OrganizationCode { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}