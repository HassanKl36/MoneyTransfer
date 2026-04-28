using MoneyTransfer.Application.Services.Clients;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
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
                page.Margin(24);
                page.Size(PageSizes.A4.Landscape());

                page.DefaultTextStyle(x => x.FontSize(10));

                page.Content().Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Text($"Statement - {statement.ClientName}")
                        .FontSize(18)
                        .Bold();

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"From: {FormatDate(statement.FromDate)}");
                        row.RelativeItem().Text($"To: {FormatDate(statement.ToDate)}");
                        row.RelativeItem().AlignRight().Text($"As Of: {statement.AsOfDate:dd/MM/yyyy HH:mm}");
                    });

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Opening Balance: {FormatCurrency(statement.Summary.OpeningBalance)}");
                        row.RelativeItem().Text($"Closing Balance: {FormatCurrency(statement.Summary.ClosingBalance)}");
                    });

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(78);   // Date
                            columns.RelativeColumn(1.4f); // Project
                            columns.ConstantColumn(82);   // Type
                            columns.ConstantColumn(82);   // Amount
                            columns.ConstantColumn(96);   // Balance
                            columns.ConstantColumn(118);  // Reference
                            columns.RelativeColumn(1.8f); // Notes
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCellStyle).Text("Date");
                            header.Cell().Element(HeaderCellStyle).Text("Project");
                            header.Cell().Element(HeaderCellStyle).Text("Type");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Amount");
                            header.Cell().Element(HeaderCellStyle).AlignRight().Text("Balance");
                            header.Cell().Element(HeaderCellStyle).Text("Reference");
                            header.Cell().Element(HeaderCellStyle).Text("Notes");
                        });

                        foreach (var entry in statement.Entries)
                        {
                            table.Cell().Element(BodyCellStyle).Text(entry.OccurredAt.ToString("dd/MM/yyyy"));
                            table.Cell().Element(BodyCellStyle).Text(entry.ProjectName ?? string.Empty);
                            table.Cell().Element(BodyCellStyle).Text(entry.Type.ToString());
                            table.Cell().Element(BodyCellStyle).AlignRight().Text(FormatCurrency(entry.Amount));
                            table.Cell().Element(BodyCellStyle).AlignRight().Text(FormatCurrency(entry.RunningBalance));
                            table.Cell().Element(BodyCellStyle).Text(entry.Reference ?? string.Empty);
                            table.Cell().Element(BodyCellStyle).Text(entry.Notes ?? string.Empty);
                        }
                    });
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatDate(DateTime? value)
    {
        return value.HasValue ? value.Value.ToString("dd/MM/yyyy") : "All";
    }

    private static string FormatCurrency(decimal value)
    {
        return $"${value:0.00}";
    }

    private static IContainer HeaderCellStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten1)
            .Background(Colors.Grey.Lighten3)
            .PaddingVertical(6)
            .PaddingHorizontal(6);
    }

    private static IContainer BodyCellStyle(IContainer container)
    {
        return container
            .Border(1)
            .BorderColor(Colors.Grey.Lighten2)
            .PaddingVertical(5)
            .PaddingHorizontal(6);
    }
}