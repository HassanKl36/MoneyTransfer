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

    public HomeController(IClientService clientService)
    {
        _clientService = clientService;
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
}