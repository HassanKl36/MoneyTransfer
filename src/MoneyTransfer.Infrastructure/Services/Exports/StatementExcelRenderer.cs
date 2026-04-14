using ClosedXML.Excel;
using MoneyTransfer.Application.Services.Clients;

namespace MoneyTransfer.Infrastructure.Services.Exports;

public sealed class StatementExcelRenderer : IStatementExcelRenderer
{
    public byte[] Render(ClientStatementDto statement)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Statement");

        int row = 1;

        ws.Cell(row++, 1).Value = $"Statement - {statement.ClientName}";
        ws.Cell(row++, 1).Value = $"From: {statement.FromDate:dd/MM/yyyy}";
        ws.Cell(row++, 1).Value = $"To: {statement.ToDate:dd/MM/yyyy}";
        ws.Cell(row++, 1).Value = $"Opening Balance: {statement.Summary.OpeningBalance}";
        ws.Cell(row++, 1).Value = $"Closing Balance: {statement.Summary.ClosingBalance}";

        row++;

        ws.Cell(row, 1).Value = "Date";
        ws.Cell(row, 2).Value = "Project";
        ws.Cell(row, 3).Value = "Type";
        ws.Cell(row, 4).Value = "Amount";
        ws.Cell(row, 5).Value = "Running Balance";

        row++;

        foreach (var e in statement.Entries)
        {
            ws.Cell(row, 1).Value = e.OccurredAt;
            ws.Cell(row, 2).Value = e.ProjectName;
            ws.Cell(row, 3).Value = e.Type.ToString();
            ws.Cell(row, 4).Value = e.Amount;
            ws.Cell(row, 5).Value = e.RunningBalance;
            row++;
        }

        ws.Column(1).Style.DateFormat.Format = "dd/MM/yyyy";
        ws.Column(1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}