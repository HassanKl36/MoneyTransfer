namespace MoneyTransfer.Application.Services.OrgUsers;

public sealed class OrgUserCreateDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    // Must be "Admin" or "Staff"
    public string Role { get; set; } = string.Empty;
}