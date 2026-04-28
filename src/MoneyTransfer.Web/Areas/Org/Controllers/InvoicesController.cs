using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Common.Exceptions;
using MoneyTransfer.Application.Services.Invoices;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Policy = "OrgPortal")]
public sealed class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid projectId,
        string? backUrl,
        string? backText,
        CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetByProjectAsync(projectId, cancellationToken);

        ViewBag.ProjectId = projectId;
        ViewBag.BackUrl = backUrl;
        ViewBag.BackText = backText;

        return View(invoices);
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
            var model = await _invoiceService.InitializeCreateAsync(projectId, cancellationToken);

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
        InvoiceCreateDto model,
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
            await _invoiceService.CreateAsync(model, cancellationToken);

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
            await _invoiceService.VoidAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Invoice voided successfully.";
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