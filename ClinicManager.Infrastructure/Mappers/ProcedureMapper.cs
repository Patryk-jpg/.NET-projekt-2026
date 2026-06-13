using ClinicManager.Core.DTOs;
using ClinicManager.Core.Models;
using Riok.Mapperly.Abstractions;

namespace ClinicManager.Infrastructure.Mappers;

/// <summary>
/// Mapperly mapper miedzy <see cref="Procedure"/> a DTO.
/// </summary>
[Mapper]
public partial class ProcedureMapper
{
    public partial ProcedureDto ToDto(Procedure procedure);

    [MapperIgnoreTarget(nameof(Procedure.Id))]
    public partial Procedure ToEntity(ProcedureFormDto form);

    [MapperIgnoreTarget(nameof(Procedure.Id))]
    public partial void Apply(ProcedureFormDto form, Procedure procedure);
}
