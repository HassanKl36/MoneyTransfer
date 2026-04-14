using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Common.Models;

namespace MoneyTransfer.Infrastructure.Services.Authentication;

public sealed class ApiKeyHasher : IApiKeyHasher
{
    private readonly ApiKeyOptions _options;

    public ApiKeyHasher(IOptions<ApiKeyOptions> options)
    {
        _options = options.Value;
    }

    public string Hash(string rawApiKey)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            throw new InvalidOperationException("API key is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.HmacSecret))
        {
            throw new InvalidOperationException("API key HMAC secret is not configured.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(_options.HmacSecret);
        var valueBytes = Encoding.UTF8.GetBytes(rawApiKey.Trim());

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(valueBytes);

        return Convert.ToHexString(hashBytes);
    }

    public string GetKeyPrefix(string rawApiKey)
    {
        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            throw new InvalidOperationException("API key is required.");
        }

        var normalized = rawApiKey.Trim();
        return normalized.Length <= 8
            ? normalized
            : normalized[..8];
    }
}