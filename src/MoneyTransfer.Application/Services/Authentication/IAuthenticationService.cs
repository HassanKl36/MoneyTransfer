namespace MoneyTransfer.Application.Services.Authentication;

public interface IAuthenticationService
{
    Task<RegisterOrganizationResult> RegisterOrganizationAsync(RegisterOrganizationRequest request);
}