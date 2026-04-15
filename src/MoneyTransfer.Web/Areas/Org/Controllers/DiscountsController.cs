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
    public async Task<IActionResult> Index(Guid projectId, CancellationToken cancellationToken)
    {
        var discounts = await _discountService.GetByProjectAsync(projectId, cancellationToken);

        ViewBag.ProjectId = projectId;
        return View(discounts);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _discountService.InitializeCreateAsync(projectId, cancellationToken);
            return View(model);
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Index), new { projectId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DiscountCreateDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _discountService.CreateAsync(model, cancellationToken);
            return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }
}