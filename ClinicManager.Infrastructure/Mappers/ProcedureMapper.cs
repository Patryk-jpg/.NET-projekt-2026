using ClinicManager.Core.DTOs;
using ClinicManager.Core.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Infrastructure.Mappers;

[Mapper]
public partial class ProcedureMapper
{
    [MapperIgnoreSource(nameof(ProcedurePerformed.Visit))]
    [MapperIgnoreSource(nameof(ProcedurePerformed.PrescribedMedications))]
    public partial ProcedurePerformedDto ToDto(ProcedurePerformed procedure);

    [MapperIgnoreTarget(nameof(ProcedurePerformed.Id))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.VisitId))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.Visit))]
    [MapperIgnoreTarget(nameof(ProcedurePerformed.PrescribedMedications))]
    public partial ProcedurePerformed ToEntity(ProcedurePerformedFormDto form);

    public IReadOnlyList<ProcedurePerformedDto> ToDtoList(IEnumerable<ProcedurePerformed> procedures) =>
        procedures.Select(ToDto).ToList();
}
