using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

/// <summary>
/// Katalog procedur medycznych: CRUD + wyszukiwanie po nazwie.
/// </summary>
public interface IProcedureService
{
    Task<IReadOnlyList<ProcedureDto>> SearchAsync(string? query, CancellationToken cancellationToken = default);

    Task<ProcedureDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProcedureDto> CreateAsync(ProcedureFormDto form, CancellationToken cancellationToken = default);

    Task<ProcedureDto?> UpdateAsync(int id, ProcedureFormDto form, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
