using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Common.Exceptions;
using MoneyTransfer.Application.Services.Discounts;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Policy = "OrgPortal")]
public sealed class DiscountsController : Controller
{
    private readonly IDiscountService _discountService;

    public DiscountsController(IDiscountService discountService)
    {
        _discountService = discountService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid projectId,
        string? backUrl,
        string? backText,
        CancellationToken cancellationToken)
    {
        var discounts = await _discountService.GetByProjectAsync(projectId, cancellationToken);

        ViewBag.ProjectId = projectId;
        ViewBag.BackUrl = backUrl;
        ViewBag.BackText = backText;

        return View(discounts);
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
            var model = await _discountService.InitializeCreateAsync(projectId, cancellationToken);

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
            return RedirectToAction(nameof(Index), new { projectId, backUrl, backText });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        DiscountCreateDto model,
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
            await _discountService.CreateAsync(model, cancellationToken);

            return RedirectToAction(nameof(Index), new
            {
                projectId = model.ProjectId,
                backUrl,
                backText
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Void(
        Guid id,
        Guid projectId,
        string? backUrl,
        string? backText,
        CancellationToken cancellationToken)
    {
        try
        {
            await _discountService.VoidAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Discount voided successfully.";
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new
        {
            projectId,
            backUrl,
            backText
        });
    }
}