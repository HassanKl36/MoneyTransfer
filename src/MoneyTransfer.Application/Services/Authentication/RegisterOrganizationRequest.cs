namespace MoneyTransfer.Application.Services.Authentication;

public sealed class RegisterOrganizationRequest
{
    public string OrganizationName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}