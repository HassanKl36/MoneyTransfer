using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Infrastructure.Data;

namespace MoneyTransfer.Web.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ApiKeyAuthorizeAttribute : Attribute, IAsyncActionFilter
{
    private const string HeaderName = "X-API-KEY";

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!httpContext.Request.Headers.TryGetValue(HeaderName, out var headerValues))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "Missing API key."
            });
            return;
        }

        var rawApiKey = headerValues.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(rawApiKey))
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "Missing API key."
            });
            return;
        }

        var services = httpContext.RequestServices;
        var dbContext = services.GetRequiredService<MoneyTransferDbContext>();
        var apiKeyHasher = services.GetRequiredService<IApiKeyHasher>();
        var requestContextOverride = services.GetRequiredService<IRequestContextOverride>();

        var hashedKey = apiKeyHasher.Hash(rawApiKey);

        var apiKey = await dbContext.ApiKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.HashedKey == hashedKey && x.RevokedAt == null,
                httpContext.RequestAborted);

        if (apiKey is null)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                error = "Invalid API key."
            });
            return;
        }

        requestContextOverride.OrganizationId = apiKey.OrganizationId;
        requestContextOverride.UserId = "API";

        await next();
    }
}