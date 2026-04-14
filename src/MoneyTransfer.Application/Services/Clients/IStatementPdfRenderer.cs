namespace MoneyTransfer.Application.Services.Clients;

public interface IStatementPdfRenderer
{
    byte[] Render(ClientStatementDto statement);
}