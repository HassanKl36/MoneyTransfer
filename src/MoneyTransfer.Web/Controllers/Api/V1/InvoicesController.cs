using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.Invoices;
using MoneyTransfer.Web.Infrastructure;
using MoneyTransfer.Web.Models.Api;

namespace MoneyTransfer.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/invoices")]
[ApiKeyAuthorize]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateInvoiceApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _invoiceService.CreateAsync(
                new InvoiceCreateDto
                {
                    ProjectId = request.ProjectId,
                    Amount = request.Amount,
                    Date = request.Date,
                    Description = request.Description
                },
                cancellationToken);

            return Ok(new
            {
                id = result.Id,
                invoiceNumber = result.InvoiceNumber
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new
            {
                error = ex.Message
            });
        }
    }
}