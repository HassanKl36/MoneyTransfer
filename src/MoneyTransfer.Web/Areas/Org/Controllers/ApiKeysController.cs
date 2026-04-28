using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.ApiKeys;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Roles = "Admin")]
public sealed class ApiKeysController : Controller
{
    private readonly IApiKeyManagementService _apiKeyManagementService;

    public ApiKeysController(IApiKeyManagementService apiKeyManagementService)
    {
        _apiKeyManagementService = apiKeyManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var apiKey = await _apiKeyManagementService.GetCurrentAsync(cancellationToken);
        return View(apiKey);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(CancellationToken cancellationToken)
    {
        try
        {
            var generatedKey = await _apiKeyManagementService.GenerateAsync(cancellationToken);

            TempData["GeneratedApiKey"] = generatedKey.RawKey;
            TempData["GeneratedApiKeyPrefix"] = generatedKey.KeyPrefix;
            TempData["SuccessMessage"] = "API key generated successfully. Copy it now; it will not be shown again.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _apiKeyManagementService.RevokeAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "API key revoked successfully.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}