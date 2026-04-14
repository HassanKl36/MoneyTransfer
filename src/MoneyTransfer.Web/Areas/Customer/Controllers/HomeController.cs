using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.Clients;
using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerPortal")]
public class HomeController : Controller
{
    private readonly IClientService _clientService;
    private readonly IStatementPdfRenderer _pdfRenderer;
    private readonly IStatementExcelRenderer _excelRenderer;

    public HomeController(
        IClientService clientService,
        IStatementPdfRenderer pdfRenderer,
        IStatementExcelRenderer excelRenderer)
    {
        _clientService = clientService;
        _pdfRenderer = pdfRenderer;
        _excelRenderer = excelRenderer;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _clientService.GetClientOverviewAsync(cancellationToken);

        if (result is null)
            return Forbid();

        return View(result);
    }

    public async Task<IActionResult> Transactions(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? projectId,
        LedgerEntryType? transactionType,
        CancellationToken cancellationToken)
    {
        var result = await _clientService.GetClientLedgerAsync(
            fromDate,
            toDate,
            projectId,
            transactionType,
            cancellationToken);

        if (result is null)
            return Forbid();

        return View(result);
    }

    [HttpGet]
    public async Task<IActionResult> ExportPdf(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? projectId,
        LedgerEntryType? transactionType,
        CancellationToken cancellationToken)
    {
        var statement = await _clientService.GetClientStatementAsync(
            fromDate,
            toDate,
            projectId,
            transactionType,
            cancellationToken);

        if (statement is null)
            return Forbid();

        var bytes = _pdfRenderer.Render(statement);
        var fileName = $"Statement_{statement.ClientName}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.pdf";

        return File(bytes, "application/pdf", fileName);
    }

    [HttpGet]
    public async Task<IActionResult> ExportExcel(
        DateTime? fromDate,
        DateTime? toDate,
        Guid? projectId,
        LedgerEntryType? transactionType,
        CancellationToken cancellationToken)
    {
        var statement = await _clientService.GetClientStatementAsync(
            fromDate,
            toDate,
            projectId,
            transactionType,
            cancellationToken);

        if (statement is null)
            return Forbid();

        var bytes = _excelRenderer.Render(statement);
        var fileName = $"Statement_{statement.ClientName}_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";

        return File(
            bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }
}