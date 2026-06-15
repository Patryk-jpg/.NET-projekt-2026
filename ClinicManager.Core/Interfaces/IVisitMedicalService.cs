using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

public interface IVisitMedicalService
{
    Task<IReadOnlyList<PrescribedMedicationDto>> GetPrescriptionsForVisitAsync(
        int visitId,
        CancellationToken cancellationToken = default);

    Task<PrescribedMedicationDto> AddPrescriptionAsync(
        int visitId,
        PrescribedMedicationFormDto form,
        CancellationToken cancellationToken = default);

    Task<decimal> GetPrescriptionTotalCostAsync(
        int visitId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClinicalNoteDto>> GetNotesForVisitAsync(
        int visitId,
        CancellationToken cancellationToken = default);

    Task<ClinicalNoteDto> AddNoteAsync(
        int visitId,
        ClinicalNoteFormDto form,
        string authorId,
        CancellationToken cancellationToken = default);
}
