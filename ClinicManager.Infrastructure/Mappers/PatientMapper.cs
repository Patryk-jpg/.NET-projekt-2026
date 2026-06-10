using ClinicManager.Core.DTOs;
using ClinicManager.Core.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Infrastructure.Mappers;

/// <summary>
/// Mapperly mapper miedzy encja <see cref="Patient"/> a DTO. Pomijamy pola sterujace
/// cyklem zycia rekordu (Id, IsDeleted, CreatedAt) w obie strony, bo sa zarzadzane
/// przez baze i serwis, a nie przez formularz.
/// </summary>
[Mapper]
public partial class PatientMapper
{
    [MapperIgnoreSource(nameof(Patient.MedicalRecord))]
    [MapperIgnoreSource(nameof(Patient.Visits))]
    public partial PatientDto ToDto(Patient patient);

    [MapperIgnoreSource(nameof(Patient.Id))]
    [MapperIgnoreSource(nameof(Patient.IsDeleted))]
    [MapperIgnoreSource(nameof(Patient.CreatedAt))]
    [MapperIgnoreSource(nameof(Patient.MedicalRecord))]
    [MapperIgnoreSource(nameof(Patient.Visits))]
    public partial PatientFormDto ToForm(Patient patient);

    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
    [MapperIgnoreTarget(nameof(Patient.MedicalRecord))]
    [MapperIgnoreTarget(nameof(Patient.Visits))]
    public partial Patient ToEntity(PatientFormDto form);

    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
    [MapperIgnoreTarget(nameof(Patient.MedicalRecord))]
    [MapperIgnoreTarget(nameof(Patient.Visits))]
    public partial void Apply(PatientFormDto form, Patient patient);
}
