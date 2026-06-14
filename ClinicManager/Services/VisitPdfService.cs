using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ClinicManager.Services;

public class VisitPdfService(
    IVisitService visitService,
    IProcedureService procedureService,
    IVisitMedicalService visitMedicalService) : IVisitPdfService
{
    public async Task<byte[]> GenerateVisitCardAsync(int visitId, CancellationToken cancellationToken = default)
    {
        var visit = await visitService.GetByIdAsync(visitId, cancellationToken)
            ?? throw new InvalidOperationException($"Wizyta {visitId} nie istnieje.");

        if (visit.Status != VisitStatus.Completed)
        {
            throw new InvalidOperationException(
                $"PDF mozna wygenerowac tylko dla wizyty w statusie '{VisitStatusLabels.GetLabel(VisitStatus.Completed)}'.");
        }

        var procedures = await procedureService.GetForVisitAsync(visitId, cancellationToken);
        var prescriptions = await visitMedicalService.GetPrescriptionsForVisitAsync(visitId, cancellationToken);
        var notes = await visitMedicalService.GetNotesForVisitAsync(visitId, cancellationToken);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(style => style.FontSize(10));

                page.Header()
                    .Column(column =>
                    {
                        column.Item().Text("Karta wizyty")
                            .FontSize(22)
                            .SemiBold()
                            .FontColor(Colors.Blue.Darken2);
                        column.Item().Text($"Wizyta #{visit.Id}")
                            .FontSize(11)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Content()
                    .PaddingVertical(20)
                    .Column(column =>
                    {
                        column.Spacing(14);
                        column.Item().Element(content => VisitSummary(content, visit));
                        column.Item().Element(content => ProcedureTable(content, procedures));
                        column.Item().Element(content => PrescriptionTable(content, prescriptions));
                        column.Item().Element(content => NotesSection(content, notes));
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(text =>
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

    private static void VisitSummary(IContainer container, VisitDto visit)
    {
        container.Column(column =>
        {
            column.Spacing(4);
            column.Item().Text("Dane wizyty").FontSize(14).SemiBold();
            column.Item().Text($"Pacjent: {visit.Patient.FirstName} {visit.Patient.LastName}, PESEL {visit.Patient.Pesel}");
            column.Item().Text($"Lekarz: {visit.Doctor.FirstName} {visit.Doctor.LastName}, {visit.Doctor.Specialization}");
            column.Item().Text($"Data wizyty: {visit.ScheduledAt:yyyy-MM-dd HH:mm}");
            column.Item().Text($"Status: {VisitStatusLabels.GetLabel(visit.Status)}");
        });
    }

    private static void ProcedureTable(IContainer container, IReadOnlyList<ProcedurePerformedDto> procedures)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("Procedury").FontSize(14).SemiBold();

            if (procedures.Count == 0)
            {
                column.Item().Text("Brak procedur.");
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.ConstantColumn(90);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Opis");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Koszt");
                });

                foreach (var procedure in procedures)
                {
                    table.Cell().Element(BodyCell).Text(procedure.Description);
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatCost(procedure.ServiceCost));
                }
            });
        });
    }

    private static void PrescriptionTable(IContainer container, IReadOnlyList<PrescribedMedicationDto> prescriptions)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("Leki").FontSize(14).SemiBold();

            if (prescriptions.Count == 0)
            {
                column.Item().Text("Brak lekow.");
                return;
            }

            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.RelativeColumn(2);
                    columns.ConstantColumn(45);
                    columns.ConstantColumn(75);
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderCell).Text("Lek");
                    header.Cell().Element(HeaderCell).Text("Procedura");
                    header.Cell().Element(HeaderCell).Text("Dawkowanie");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Ilosc");
                    header.Cell().Element(HeaderCell).AlignRight().Text("Koszt");
                });

                foreach (var prescription in prescriptions)
                {
                    table.Cell().Element(BodyCell).Text(prescription.MedicationName);
                    table.Cell().Element(BodyCell).Text(prescription.ProcedureDescription);
                    table.Cell().Element(BodyCell).Text(prescription.Dosage);
                    table.Cell().Element(BodyCell).AlignRight().Text(prescription.Quantity.ToString());
                    table.Cell().Element(BodyCell).AlignRight().Text(FormatCost(prescription.TotalCost));
                }
            });
        });
    }

    private static void NotesSection(IContainer container, IReadOnlyList<ClinicalNoteDto> notes)
    {
        container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text("Notatki i zalecenia").FontSize(14).SemiBold();

            if (notes.Count == 0)
            {
                column.Item().Text("Brak notatek klinicznych.");
                return;
            }

            foreach (var note in notes)
            {
                column.Item()
                    .Border(1)
                    .BorderColor(Colors.Grey.Lighten2)
                    .Padding(8)
                    .Column(noteColumn =>
                    {
                        noteColumn.Item().Text($"{note.Timestamp:yyyy-MM-dd HH:mm} | Autor: {note.AuthorId}")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                        noteColumn.Item().Text(note.Content);
                    });
            }
        });
    }

    private static IContainer HeaderCell(IContainer container) =>
        container.Background(Colors.Grey.Lighten3).Padding(5).DefaultTextStyle(style => style.SemiBold());

    private static IContainer BodyCell(IContainer container) =>
        container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);

    private static string FormatCost(decimal value) => $"{value:0.00} zl";
}
