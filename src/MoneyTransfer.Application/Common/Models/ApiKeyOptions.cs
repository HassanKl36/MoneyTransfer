namespace MoneyTransfer.Application.Common.Models;

public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKeys";

    public string HmacSecret { get; set; } = string.Empty;
}