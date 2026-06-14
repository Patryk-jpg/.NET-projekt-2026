using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Services;

public class CostReportPdfService(ICostReportService costReportService) : ICostReportPdfService
{
    public async Task<byte[]> GenerateAsync(
        CostReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        var report = await costReportService.GenerateAsync(filter, cancellationToken);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(35);
                page.DefaultTextStyle(style => style.FontSize(9));

                page.Header().Column(column =>
                {
                    column.Item().Text("Raport kosztow swiadczen")
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);
                    column.Item().Text(FilterDescription(report.Filter))
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(16).Column(column =>
                {
                    column.Spacing(12);
                    column.Item().Element(content => Summary(content, report));
                    column.Item().Element(content => RowsTable(content, report.Rows));
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Wygenerowano: ");
                    text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                    text.Span(" | Strona ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static void Summary(IContainer container, CostReportDto report)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text($"Procedury: {FormatCost(report.TotalProcedureCost)}").SemiBold();
            row.RelativeItem().Text($"Leki: {FormatCost(report.TotalMedicationCost)}").SemiBold();
            row.RelativeItem().Text($"Razem: {FormatCost(report.TotalCost)}").SemiBold();
        });
    }

    private static void RowsTable(IContainer container, IReadOnlyList<CostReportRowDto> rows)
    {
        if (rows.Count == 0)
        {
            container.Text("Brak wizyt dla wybranych filtrow.");
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(70);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.ConstantColumn(75);
                columns.ConstantColumn(75);
                columns.ConstantColumn(75);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Data");
                header.Cell().Element(HeaderCell).Text("Pacjent");
                header.Cell().Element(HeaderCell).Text("Lekarz");
                header.Cell().Element(HeaderCell).AlignRight().Text("Procedury");
                header.Cell().Element(HeaderCell).AlignRight().Text("Leki");
                header.Cell().Element(HeaderCell).AlignRight().Text("Razem");
            });

            foreach (var row in rows)
            {
                table.Cell().Element(BodyCell).Text(row.ScheduledAt.ToString("yyyy-MM-dd"));
                table.Cell().Element(BodyCell).Text($"{row.PatientName} ({row.PatientPesel})");
                table.Cell().Element(BodyCell).Text($"{row.DoctorName}, {row.DoctorSpecialization}");
                table.Cell().Element(BodyCell).AlignRight().Text(FormatCost(row.ProcedureCost));
                table.Cell().Element(BodyCell).AlignRight().Text(FormatCost(row.MedicationCost));
                table.Cell().Element(BodyCell).AlignRight().Text(FormatCost(row.TotalCost));
            }
        });
    }

    private static string FilterDescription(CostReportFilterDto filter)
    {
        var parts = new List<string>();

        if (filter.PatientId is not null)
        {
            parts.Add($"pacjent ID {filter.PatientId}");
        }

        if (filter.DoctorId is not null)
        {
            parts.Add($"lekarz ID {filter.DoctorId}");
        }

        if (filter.Year is not null && filter.Month is not null)
        {
            parts.Add($"miesiac {filter.Year:0000}-{filter.Month:00}");
        }
        else if (filter.Year is not null)
        {
            parts.Add($"rok {filter.Year}");
        }

        return parts.Count == 0 ? "Filtry: wszystkie wizyty" : $"Filtry: {string.Join(", ", parts)}";
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(5).DefaultTextStyle(style => style.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

    private static string FormatCost(decimal value) => $"{value:0.00} zl";
}
