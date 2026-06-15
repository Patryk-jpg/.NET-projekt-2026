using ClinicManager.Core.DTOs;
using ClinicManager.Core.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Infrastructure.Mappers;

/// <summary>
/// Mapperly mapper miedzy <see cref="Visit"/> a DTO. Prywatne metody
/// <c>ToPatientSummary</c> i <c>ToDoctorSummary</c> sa automatycznie
/// uzywane przez Mapperly przy mapowaniu zagniezdonych obiektow w <c>ToDto</c>.
/// </summary>
[Mapper]
public partial class VisitMapper
{
    [MapperIgnoreSource(nameof(Visit.PatientId))]
    [MapperIgnoreSource(nameof(Visit.DoctorId))]
    [MapperIgnoreSource(nameof(Visit.Procedures))]
    [MapperIgnoreSource(nameof(Visit.Notes))]
    public partial VisitDto ToDto(Visit visit);

    [MapperIgnoreSource(nameof(Doctor.UserId))]
    [MapperIgnoreSource(nameof(Doctor.Visits))]
    public partial DoctorOptionDto ToOption(Doctor doctor);

    public IReadOnlyList<VisitDto> ToDtoList(IEnumerable<Visit> visits) =>
        visits.Select(ToDto).ToList();

    [MapperIgnoreSource(nameof(Patient.InsuranceNumber))]
    [MapperIgnoreSource(nameof(Patient.DateOfBirth))]
    [MapperIgnoreSource(nameof(Patient.Phone))]
    [MapperIgnoreSource(nameof(Patient.Email))]
    [MapperIgnoreSource(nameof(Patient.IsDeleted))]
    [MapperIgnoreSource(nameof(Patient.DeletedAt))]
    [MapperIgnoreSource(nameof(Patient.AnonymizedAt))]
    [MapperIgnoreSource(nameof(Patient.CreatedAt))]
    [MapperIgnoreSource(nameof(Patient.MedicalRecord))]
    [MapperIgnoreSource(nameof(Patient.Visits))]
    private partial PatientSummaryDto ToPatientSummary(Patient patient);

    [MapperIgnoreSource(nameof(Doctor.UserId))]
    [MapperIgnoreSource(nameof(Doctor.Visits))]
    private partial DoctorSummaryDto ToDoctorSummary(Doctor doctor);
}
