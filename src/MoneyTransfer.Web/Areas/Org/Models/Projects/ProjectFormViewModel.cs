using Microsoft.AspNetCore.Mvc.Rendering;
using MoneyTransfer.Application.Services.Projects;

namespace MoneyTransfer.Web.Areas.Org.Models.Projects;

public sealed class ProjectFormViewModel
{
    public ProjectEditDto Project { get; set; } = new();

    public List<SelectListItem> Clients { get; set; } = new();
}