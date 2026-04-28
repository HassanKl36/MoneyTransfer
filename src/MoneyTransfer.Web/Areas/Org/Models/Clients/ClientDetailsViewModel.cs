using MoneyTransfer.Application.Services.Clients;

namespace MoneyTransfer.Web.Areas.Org.Models.Clients;

public sealed class ClientDetailsViewModel
{
    public ClientDetailsDto Client { get; set; } = new();

    public ClientStatementDto Statement { get; set; } = new();
}