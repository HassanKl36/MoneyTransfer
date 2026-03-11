using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MoneyTransfer.Web.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize(Policy = "CustomerPortal")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}   