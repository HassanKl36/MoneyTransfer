namespace MoneyTransfer.Application.Services.ApiKeys;

public interface IApiKeyManagementService
{
    Task<ApiKeyDetailsDto?> GetCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<GeneratedApiKeyDto> GenerateAsync(
        CancellationToken cancellationToken = default);

    Task RevokeAsync(
        Guid apiKeyId,
        CancellationToken cancellationToken = default);
}