namespace MoneyTransfer.Application.Services.Clients;

public interface IStatementExcelRenderer
{
    byte[] Render(ClientStatementDto statement);
}