using MoneyTransfer.Application.Services.Clients;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace MoneyTransfer.Infrastructure.Services.Exports;

public sealed class StatementPdfRenderer : IStatementPdfRenderer
{
    public byte[] Render(ClientStatementDto statement)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(20);

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Text($"Statement - {statement.ClientName}")
                        .FontSize(18).Bold();

                    col.Item().Text($"From: {statement.FromDate:dd/MM/yyyy} To: {statement.ToDate:dd/MM/yyyy}");

                    col.Item().Text($"Opening Balance: {statement.Summary.OpeningBalance}");
                    col.Item().Text($"Closing Balance: {statement.Summary.ClosingBalance}");

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(80);
                            columns.RelativeColumn();
                            columns.ConstantColumn(80);
                            columns.ConstantColumn(100);
                            columns.ConstantColumn(100);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Date");
                            header.Cell().Text("Project");
                            header.Cell().Text("Type");
                            header.Cell().Text("Amount");
                            header.Cell().Text("Balance");
                        });

                        foreach (var e in statement.Entries)
                        {
                            table.Cell().Text(e.OccurredAt.ToString("dd/MM/yyyy"));
                            table.Cell().Text(e.ProjectName);
                            table.Cell().Text(e.Type.ToString());
                            table.Cell().Text(e.Amount.ToString("0.00"));
                            table.Cell().Text(e.RunningBalance.ToString("0.00"));
                        }
                    });
                });
            });
        });

        return document.GeneratePdf();
    }
}