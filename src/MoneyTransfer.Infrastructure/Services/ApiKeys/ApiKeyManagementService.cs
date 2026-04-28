using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.ApiKeys;
using MoneyTransfer.Domain.Entities;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Infrastructure.Services.ApiKeys;

public sealed class ApiKeyManagementService : IApiKeyManagementService
{
    private readonly MoneyTransferDbContext _dbContext;
    private readonly ICurrentOrganization _currentOrganization;
    private readonly IApiKeyHasher _apiKeyHasher;

    public ApiKeyManagementService(
        MoneyTransferDbContext dbContext,
        ICurrentOrganization currentOrganization,
        IApiKeyHasher apiKeyHasher)
    {
        _dbContext = dbContext;
        _currentOrganization = currentOrganization;
        _apiKeyHasher = apiKeyHasher;
    }

    public async Task<ApiKeyDetailsDto?> GetCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        return await _dbContext.ApiKeys
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ApiKeyDetailsDto
            {
                Id = x.Id,
                KeyPrefix = x.KeyPrefix,
                CreatedAt = x.CreatedAt,
                RevokedAt = x.RevokedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<GeneratedApiKeyDto> GenerateAsync(
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var hasActiveKey = await _dbContext.ApiKeys
            .AnyAsync(
                x => x.OrganizationId == organizationId
                  && x.RevokedAt == null,
                cancellationToken);

        if (hasActiveKey)
        {
            throw new InvalidOperationException(
                "This organization already has an active API key. Revoke it before generating a new one.");
        }

        var rawKey = GenerateRawKey();
        var keyPrefix = _apiKeyHasher.GetKeyPrefix(rawKey);
        var hashedKey = _apiKeyHasher.Hash(rawKey);

        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            KeyPrefix = keyPrefix,
            HashedKey = hashedKey,
            CreatedAt = DateTime.UtcNow,
            RevokedAt = null
        };

        _dbContext.ApiKeys.Add(apiKey);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new GeneratedApiKeyDto
        {
            RawKey = rawKey,
            KeyPrefix = keyPrefix
        };
    }

    public async Task RevokeAsync(
        Guid apiKeyId,
        CancellationToken cancellationToken = default)
    {
        var organizationId = await _currentOrganization.GetRequiredOrganizationIdAsync(cancellationToken);

        var apiKey = await _dbContext.ApiKeys
            .FirstOrDefaultAsync(
                x => x.Id == apiKeyId
                  && x.OrganizationId == organizationId,
                cancellationToken);

        if (apiKey is null)
        {
            throw new InvalidOperationException("API key was not found.");
        }

        if (apiKey.RevokedAt is not null)
        {
            return;
        }

        apiKey.RevokedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateRawKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return $"mt_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }
}