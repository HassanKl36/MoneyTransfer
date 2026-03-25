using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.Invoices;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Roles = "Admin")]
public sealed class InvoicesController : Controller
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(Guid projectId, CancellationToken cancellationToken)
    {
        var invoices = await _invoiceService.GetByProjectAsync(projectId, cancellationToken);

        ViewBag.ProjectId = projectId;
        return View(invoices);
    }

    [HttpGet]
    public async Task<IActionResult> Create(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _invoiceService.InitializeCreateAsync(projectId, cancellationToken);
            return View(model);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InvoiceCreateDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await _invoiceService.CreateAsync(model, cancellationToken);
            return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }
}