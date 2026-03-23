namespace MoneyTransfer.Application.Services.Authentication;

public sealed class RegisterOrganizationResult
{
    public bool Succeeded { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? UserId { get; set; }
    public List<string> Errors { get; set; } = [];
}