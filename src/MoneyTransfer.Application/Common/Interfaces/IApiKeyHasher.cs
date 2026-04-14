namespace MoneyTransfer.Application.Common.Interfaces;

public interface IApiKeyHasher
{
    string Hash(string rawApiKey);

    string GetKeyPrefix(string rawApiKey);
}