using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Common.Exceptions;
using MoneyTransfer.Application.Services.Adjustments;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Policy = "OrgPortal")]
[Authorize(Roles = "Admin")]
public sealed class AdjustmentsController : Controller
{
    private readonly IAdjustmentService _adjustmentService;

    public AdjustmentsController(IAdjustmentService adjustmentService)
    {
        _adjustmentService = adjustmentService;
    }

    [HttpGet]
    public async Task<IActionResult> Create(
        Guid projectId,
        string? backUrl,
        string? backText,
        CancellationToken cancellationToken)
    {
        try
        {
            var model = await _adjustmentService.InitializeCreateAsync(projectId, cancellationToken);

            ViewBag.BackUrl = backUrl;
            ViewBag.BackText = backText;

            return View(model);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;

            return RedirectToAction("Details", "Projects", new
            {
                id = projectId
            });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdjustmentCreateDto model,
        string? backUrl,
        string? backText,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.BackUrl = backUrl;
            ViewBag.BackText = backText;
            return View(model);
        }

        try
        {
            await _adjustmentService.CreateAsync(model, cancellationToken);

            if (!string.IsNullOrWhiteSpace(backUrl))
            {
                return LocalRedirect(backUrl);
            }

            return RedirectToAction("Details", "Projects", new
            {
                id = model.ProjectId
            });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ViewBag.BackUrl = backUrl;
            ViewBag.BackText = backText;
            return View(model);
        }
    }
}