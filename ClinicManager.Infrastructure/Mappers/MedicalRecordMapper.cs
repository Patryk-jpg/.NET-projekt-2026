using ClinicManager.Core.DTOs;
using ClinicManager.Core.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Infrastructure.Mappers;

/// <summary>
/// Mapperly mapper miedzy <see cref="MedicalRecord"/> a DTO. Pomija nawigacje do pacjenta,
/// poniewaz DTO transportuje tylko sam rekord kartoteki.
/// </summary>
[Mapper]
public partial class MedicalRecordMapper
{
    [MapperIgnoreSource(nameof(MedicalRecord.Patient))]
    public partial MedicalRecordDto ToDto(MedicalRecord record);
}
