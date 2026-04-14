using Microsoft.AspNetCore.Mvc;
using MoneyTransfer.Application.Services.Payments;
using MoneyTransfer.Web.Infrastructure;
using MoneyTransfer.Web.Models.Api;

namespace MoneyTransfer.Web.Controllers.Api.V1;

[ApiController]
[Route("api/v1/payments/client")]
[ApiKeyAuthorize]
public sealed class ClientPaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public ClientPaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateClientPaymentAllocationApiRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        try
        {
            var result = await _paymentService.CreateClientAllocationAsync(
                new ClientPaymentCreateDto
                {
                    ClientId = request.ClientId,
                    TotalAmount = request.TotalAmount,
                    Date = request.Date,
                    PaymentMethod = request.PaymentMethod,
                    Description = request.Description,
                    Allocations = request.Allocations
                        .Select(x => new ClientPaymentAllocationLineDto
                        {
                            ProjectId = x.ProjectId,
                            Amount = x.Amount
                        })
                        .ToList()
                },
                cancellationToken);

            return Ok(new
            {
                headerId = result.HeaderId,
                paymentReference = result.PaymentReference,
                paymentIds = result.PaymentIds
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