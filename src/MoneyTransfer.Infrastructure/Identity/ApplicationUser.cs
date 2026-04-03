using Microsoft.AspNetCore.Identity;
using MoneyTransfer.Domain.Entities;

namespace MoneyTransfer.Infrastructure.Identity;

/// <summary>
/// Single Identity user type for the whole system.
/// - Org users (Admin/Staff): OrganizationId set
/// - Customer users: ClientId set
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public Guid? OrganizationId { get; set; }
    public Organization? Organization { get; set; }

    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
}