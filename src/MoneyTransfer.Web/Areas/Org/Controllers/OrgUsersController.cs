using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.OrgUsers;
using MoneyTransfer.Web.Areas.Org.Models.OrgUsers;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Roles = "Admin")]
public class OrgUsersController : Controller
{
    private readonly IOrgUserService _orgUserService;

    public OrgUsersController(IOrgUserService orgUserService)
    {
        _orgUserService = orgUserService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _orgUserService.GetUsersAsync(cancellationToken);
        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new OrgUserCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        OrgUserCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _orgUserService.CreateAsync(new OrgUserCreateDto
        {
            Email = model.Email,
            FullName = model.FullName,
            Password = model.Password,
            Role = model.Role
        }, cancellationToken);

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
}