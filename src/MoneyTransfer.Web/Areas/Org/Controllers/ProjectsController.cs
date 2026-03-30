using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using MoneyTransfer.Application.Services.Clients;
using MoneyTransfer.Application.Services.Projects;
using MoneyTransfer.Web.Areas.Org.Models.Projects;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Roles = "Admin")]
public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IClientService _clientService;
    private readonly IProjectFinancialService _projectFinancialService;

    public ProjectsController(
        IProjectService projectService,
        IClientService clientService,
        IProjectFinancialService projectFinancialService)
    {
        _projectService = projectService;
        _clientService = clientService;
        _projectFinancialService = projectFinancialService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var projects = await _projectService.GetProjectsAsync(
            search: search,
            cancellationToken: cancellationToken);

        ViewBag.Search = search;
        return View(projects);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var summary = await _projectFinancialService.GetSummaryAsync(id, cancellationToken);

        if (summary is null)
        {
            return NotFound();
        }

        return View(summary);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var model = new ProjectFormViewModel
        {
            Project = new ProjectEditDto(),
            Clients = await GetClientSelectListAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Clients = await GetClientSelectListAsync(cancellationToken);
            return View(model);
        }

        await _projectService.CreateAsync(model.Project, cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var project = await _projectService.GetForEditAsync(id, cancellationToken);

        if (project is null)
        {
            return NotFound();
        }

        var model = new ProjectFormViewModel
        {
            Project = project,
            Clients = await GetClientSelectListAsync(cancellationToken)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Clients = await GetClientSelectListAsync(cancellationToken);
            return View(model);
        }

        var updated = await _projectService.UpdateAsync(model.Project, cancellationToken);

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
        var archived = await _projectService.ArchiveAsync(id, cancellationToken);

        if (!archived)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> GetClientSelectListAsync(CancellationToken cancellationToken)
    {
        var clients = await _clientService.GetClientsAsync(
            includeArchived: false,
            cancellationToken: cancellationToken);

        return clients
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            })
            .ToList();
    }
}