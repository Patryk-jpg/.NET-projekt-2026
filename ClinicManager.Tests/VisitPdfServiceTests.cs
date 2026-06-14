using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Interfaces;
using ClinicManager.Services;
using QuestPDF.Infrastructure;
using Xunit;

namespace ClinicManager.Tests;

public class VisitPdfServiceTests
{
    public VisitPdfServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task GenerateVisitCardAsync_DlaZakonczonejWizyty_ZwracaPdf()
    {
        // PDF karty wizyty powinien powstac tylko z pelnego zestawu danych wizyty:
        // dane pacjenta/lekarza, procedury, leki oraz notatki kliniczne.
        var service = BuildService(VisitStatus.Completed);

        var pdf = await service.GenerateVisitCardAsync(1);

        Assert.True(pdf.Length > 0);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf[..4]));
    }

    [Fact]
    public async Task GenerateVisitCardAsync_DlaNiezakonczonejWizyty_RzucaWyjatek()
    {
        // DoD US-11 wymaga, zeby PDF generowal sie dla wizyty zakonczonej,
        // wiec status Planned/InProgress/Cancelled blokujemy w serwisie.
        var service = BuildService(VisitStatus.InProgress);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateVisitCardAsync(1));

        Assert.Contains("Zakonczona", ex.Message);
    }

    private static VisitPdfService BuildService(VisitStatus status) =>
        new(
            new FakeVisitService(status),
            new FakeProcedureService(),
            new FakeVisitMedicalService());

    private sealed class FakeVisitService(VisitStatus status) : IVisitService
    {
        public Task<VisitDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<VisitDto?>(new VisitDto(
                id,
                new PatientSummaryDto(1, "Jan", "Kowalski", "90010112345"),
                new DoctorSummaryDto(1, "Anna", "Nowak", "Internista"),
                new DateTime(2026, 1, 2, 10, 0, 0),
                status,
                new DateTime(2026, 1, 1, 8, 0, 0)));

        public Task<IReadOnlyList<VisitDto>> SearchAsync(
            VisitStatus? status = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<VisitDto>> GetForPatientAsync(
            int patientId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<VisitDto> CreateAsync(VisitFormDto form, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<VisitDto> UpdateAsync(int id, VisitFormDto form, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<VisitDto> ChangeStatusAsync(
            int id,
            VisitStatus newStatus,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<VisitDto>> GetForDoctorAsync(
            string userId,
            DateOnly? date = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<DoctorOptionDto>> GetDoctorOptionsAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeProcedureService : IProcedureService
    {
        public Task<IReadOnlyList<ProcedurePerformedDto>> GetForVisitAsync(
            int visitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ProcedurePerformedDto>>([
                new ProcedurePerformedDto(1, visitId, "Konsultacja lekarska", 150m)
            ]);

        public Task<ProcedurePerformedDto> AddToVisitAsync(
            int visitId,
            ProcedurePerformedFormDto form,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<decimal> GetVisitTotalCostAsync(int visitId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeVisitMedicalService : IVisitMedicalService
    {
        public Task<IReadOnlyList<PrescribedMedicationDto>> GetPrescriptionsForVisitAsync(
            int visitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PrescribedMedicationDto>>([
                new PrescribedMedicationDto(1, 1, "Konsultacja lekarska", 1, "Paracetamol", 12.50m, "1 tabletka", 2, 25m)
            ]);

        public Task<IReadOnlyList<ClinicalNoteDto>> GetNotesForVisitAsync(
            int visitId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ClinicalNoteDto>>([
                new ClinicalNoteDto(1, visitId, "doctor-user-1", "Zalecenia: odpoczynek i kontrola.", new DateTime(2026, 1, 2, 10, 30, 0))
            ]);

        public Task<PrescribedMedicationDto> AddPrescriptionAsync(
            int visitId,
            PrescribedMedicationFormDto form,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<decimal> GetPrescriptionTotalCostAsync(
            int visitId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<ClinicalNoteDto> AddNoteAsync(
            int visitId,
            ClinicalNoteFormDto form,
            string authorId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
