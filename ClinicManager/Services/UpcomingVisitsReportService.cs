using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Services;

public class UpcomingVisitsReportService(IDbContextFactory<ApplicationDbContext> dbContextFactory)
    : IUpcomingVisitsReportService
{
    public async Task<byte[]> GeneratePdfAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var visits = await GetVisitsAsync(date, cancellationToken);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("Raport nadchodzacych wizyt")
                        .FontSize(20)
                        .SemiBold()
                        .FontColor(Colors.Blue.Darken2);
                    column.Item().Text($"Data: {date:yyyy-MM-dd}")
                        .FontSize(11)
                        .FontColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(16).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Text($"Liczba wizyt: {visits.Count}").SemiBold();
                    column.Item().Element(content => VisitsTable(content, visits));
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

    private async Task<IReadOnlyList<UpcomingVisitReportItemDto>> GetVisitsAsync(
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var visits = await db.Visits
            .AsNoTracking()
            .Include(visit => visit.Patient)
            .Include(visit => visit.Doctor)
            .Where(visit => visit.ScheduledAt >= start && visit.ScheduledAt < end)
            .OrderBy(visit => visit.ScheduledAt)
            .ToListAsync(cancellationToken);

        return visits.Select(visit => new UpcomingVisitReportItemDto(
            visit.Id,
            visit.ScheduledAt,
            visit.Status,
            $"{visit.Patient.FirstName} {visit.Patient.LastName}",
            visit.Patient.Pesel,
            $"{visit.Doctor.FirstName} {visit.Doctor.LastName}",
            visit.Doctor.Specialization)).ToList();
    }

    private static void VisitsTable(IContainer container, IReadOnlyList<UpcomingVisitReportItemDto> visits)
    {
        if (visits.Count == 0)
        {
            container.Text("Brak zaplanowanych wizyt na wybrany dzien.");
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.ConstantColumn(60);
                columns.RelativeColumn(2);
                columns.RelativeColumn(2);
                columns.RelativeColumn();
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Godzina");
                header.Cell().Element(HeaderCell).Text("Pacjent");
                header.Cell().Element(HeaderCell).Text("Lekarz");
                header.Cell().Element(HeaderCell).Text("Status");
            });

            foreach (var visit in visits)
            {
                table.Cell().Element(BodyCell).Text(visit.ScheduledAt.ToString("HH:mm"));
                table.Cell().Element(BodyCell).Text($"{visit.PatientName} ({visit.PatientPesel})");
                table.Cell().Element(BodyCell).Text($"{visit.DoctorName}, {visit.DoctorSpecialization}");
                table.Cell().Element(BodyCell).Text(visit.Status.ToString());
            }
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(5).DefaultTextStyle(style => style.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
}
