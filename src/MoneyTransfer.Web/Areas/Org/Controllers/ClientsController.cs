using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.Clients;
using MoneyTransfer.Domain.Enums;
using MoneyTransfer.Web.Areas.Org.Models.Clients;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Policy = "OrgPortal")]
public class ClientsController : Controller
{
    private readonly IClientService _clientService;
    private readonly IStatementPdfRenderer _pdfRenderer;
    private readonly IStatementExcelRenderer _excelRenderer;

    public ClientsController(
        IClientService clientService,
        IStatementPdfRenderer pdfRenderer,
        IStatementExcelRenderer excelRenderer)
    {
        _clientService = clientService;
        _pdfRenderer = pdfRenderer;
        _excelRenderer = excelRenderer;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? search,
        ClientStatus? status,
        CancellationToken cancellationToken)
    {
        var clients = await _clientService.GetClientsAsync(
            search: search,
            status: status,
            cancellationToken: cancellationToken);

        ViewBag.Search = search;
        ViewBag.Status = status;

        return View(clients);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? projectId,
        LedgerEntryType? transactionType,
        CancellationToken cancellationToken)
    {
        var client = await _clientService.GetDetailsAsync(id, cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        var statement = await _clientService.GetClientStatementForOrgAsync(
            id,
            fromDate,
            toDate,
            projectId,
            transactionType,
            cancellationToken);

        if (statement is null)
        {
            return NotFound();
        }

        var viewModel = new ClientDetailsViewModel
        {
            Client = client,
            Statement = statement
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(
        Guid id,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? projectId,
        LedgerEntryType? transactionType,
        CancellationToken cancellationToken)
    {
        var statement = await _clientService.GetClientStatementForOrgAsync(
            id,
            fromDate,
            toDate,
            projectId,
            transactionType,
            cancellationToken);

        if (statement is null)
        {
            return NotFound();
        }

        var bytes = _pdfRenderer.Render(statement);
        var fileName = BuildStatementFileName(statement, "pdf");

        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        Guid id,
        DateTime? fromDate,
        DateTime? toDate,
        Guid? projectId,
        LedgerEntryType? transactionType,
        CancellationToken cancellationToken)
    {
        var statement = await _clientService.GetClientStatementForOrgAsync(
            id,
            fromDate,
            toDate,
            projectId,
            transactionType,
            cancellationToken);

        if (statement is null)
        {
            return NotFound();
        }

        var bytes = _excelRenderer.Render(statement);
        var fileName = BuildStatementFileName(statement, "xlsx");

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ClientEditDto
        {
            CreatePortalAccount = false,
            HasPortalAccount = false
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientEditDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _clientService.CreateAsync(model, cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var client = await _clientService.GetForEditAsync(id, cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClientEditDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _clientService.UpdateAsync(model, cancellationToken);

        if (!result.Succeeded)
        {
            if (!result.Errors.Any())
            {
                return NotFound();
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var archived = await _clientService.ArchiveAsync(id, cancellationToken);

        if (!archived)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private static string BuildStatementFileName(ClientStatementDto statement, string extension)
    {
        var safeClientName = string.Join("_",
            statement.ClientName
                .Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var fromPart = statement.FromDate?.ToString("yyyyMMdd") ?? "all";
        var toPart = statement.ToDate?.ToString("yyyyMMdd") ?? statement.AsOfDate.ToString("yyyyMMdd");

        return $"Statement_{safeClientName}_{fromPart}_{toPart}.{extension}";
    }
}