using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoneyTransfer.Web.Areas.Org.Controllers;

[Area("Org")]
[Authorize(Policy = "OrgPortal")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}