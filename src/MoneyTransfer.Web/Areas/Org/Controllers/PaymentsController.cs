using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Common.Exceptions;
using MoneyTransfer.Application.Services.Payments;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Policy = "OrgPortal")]
public sealed class PaymentsController : Controller
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid projectId,
        string? backUrl,
        string? backText,
        CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _paymentService.GetByProjectAsync(projectId, cancellationToken);

            ViewBag.ProjectId = projectId;
            ViewBag.BackUrl = backUrl;
            ViewBag.BackText = backText;

            return View(payments);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
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
            var model = await _paymentService.InitializeCreateAsync(projectId, cancellationToken);

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
        PaymentCreateDto model,
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
            await _paymentService.CreateAsync(model, cancellationToken);

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
            await _paymentService.VoidAsync(id, cancellationToken);
            TempData["SuccessMessage"] = "Payment voided successfully.";
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

    [HttpGet]
    public async Task<IActionResult> CreateClientAllocation(Guid clientId, CancellationToken cancellationToken)
    {
        try
        {
            var model = await _paymentService.InitializeClientAllocationCreateAsync(clientId, cancellationToken);
            return View(model);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClientAllocation(ClientPaymentCreateDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var reloadedModel = await TryReloadClientAllocationModelAsync(model, cancellationToken);
            return View(reloadedModel);
        }

        try
        {
            await _paymentService.CreateClientAllocationAsync(model, cancellationToken);

            return RedirectToAction(
                "Details",
                "Clients",
                new { id = model.ClientId });
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);

            var reloadedModel = await TryReloadClientAllocationModelAsync(model, cancellationToken);
            return View(reloadedModel);
        }
    }

    private async Task<ClientPaymentCreateDto> TryReloadClientAllocationModelAsync(
        ClientPaymentCreateDto postedModel,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ReloadClientAllocationModelAsync(postedModel, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return BuildFallbackClientAllocationModel(postedModel);
        }
    }

    private async Task<ClientPaymentCreateDto> ReloadClientAllocationModelAsync(
        ClientPaymentCreateDto postedModel,
        CancellationToken cancellationToken)
    {
        var reloadedModel = await _paymentService.InitializeClientAllocationCreateAsync(
            postedModel.ClientId,
            cancellationToken);

        reloadedModel.TotalAmount = postedModel.TotalAmount;
        reloadedModel.Date = postedModel.Date;
        reloadedModel.PaymentMethod = postedModel.PaymentMethod;
        reloadedModel.Description = postedModel.Description;

        var postedAllocations = postedModel.Allocations?
            .ToDictionary(x => x.ProjectId, x => x.Amount)
            ?? new Dictionary<Guid, decimal>();

        reloadedModel.Allocations = reloadedModel.AvailableProjects
            .Select(project => new ClientPaymentAllocationLineDto
            {
                ProjectId = project.ProjectId,
                Amount = postedAllocations.TryGetValue(project.ProjectId, out var amount)
                    ? amount
                    : 0m
            })
            .ToList();

        return reloadedModel;
    }

    private static ClientPaymentCreateDto BuildFallbackClientAllocationModel(
        ClientPaymentCreateDto postedModel)
    {
        return new ClientPaymentCreateDto
        {
            ClientId = postedModel.ClientId,
            ClientName = postedModel.ClientName,
            TotalAmount = postedModel.TotalAmount,
            Date = postedModel.Date,
            PaymentMethod = postedModel.PaymentMethod,
            Description = postedModel.Description,
            AvailableProjects = Array.Empty<ClientPaymentProjectOptionDto>(),
            Allocations = postedModel.Allocations?
                .Select(x => new ClientPaymentAllocationLineDto
                {
                    ProjectId = x.ProjectId,
                    Amount = x.Amount
                })
                .ToList()
                ?? new List<ClientPaymentAllocationLineDto>()
        };
    }
}