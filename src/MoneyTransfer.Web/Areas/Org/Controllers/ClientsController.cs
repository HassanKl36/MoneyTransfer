using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.Clients;
using MoneyTransfer.Domain.Enums;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Policy = "OrgPortal")]
public class ClientsController : Controller
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
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
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var client = await _clientService.GetDetailsAsync(id, cancellationToken);

        if (client is null)
        {
            return NotFound();
        }

        return View(client);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ClientEditDto());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientEditDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await _clientService.CreateAsync(model, cancellationToken);
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

        var updated = await _clientService.UpdateAsync(model, cancellationToken);

        if (!updated)
        {
            return NotFound();
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
}