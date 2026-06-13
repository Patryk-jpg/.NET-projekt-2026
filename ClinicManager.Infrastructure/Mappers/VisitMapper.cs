using ClinicManager.Core.DTOs;
using ClinicManager.Core.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Infrastructure.Mappers;

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
    [MapperIgnoreSource(nameof(Patient.CreatedAt))]
    [MapperIgnoreSource(nameof(Patient.MedicalRecord))]
    [MapperIgnoreSource(nameof(Patient.Visits))]
    private partial PatientSummaryDto ToPatientSummary(Patient patient);

    [MapperIgnoreSource(nameof(Doctor.UserId))]
    [MapperIgnoreSource(nameof(Doctor.Visits))]
    private partial DoctorSummaryDto ToDoctorSummary(Doctor doctor);
}
