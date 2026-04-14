using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.Payments;
using MoneyTransfer.Web.Infrastructure;
using MoneyTransfer.Web.Models.Api;

namespace MoneyTransfer.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/payments/project")]
[ApiKeyAuthorize]
public sealed class ProjectPaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public ProjectPaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateProjectPaymentApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _paymentService.CreateAsync(
                new PaymentCreateDto
                {
                    ProjectId = request.ProjectId,
                    Amount = request.Amount,
                    Date = request.Date,
                    PaymentMethod = request.PaymentMethod,
                    Description = request.Description
                },
                cancellationToken);

            return Ok(new
            {
                id = result.Id,
                paymentReference = result.PaymentReference
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