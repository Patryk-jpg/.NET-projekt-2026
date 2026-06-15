using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

/// <summary>
/// Logika domenowa pacjentow: CRUD, wyszukiwanie po nazwisku/PESEL, soft delete.
/// </summary>
public interface IPatientService
{
    /// <summary>
    /// Zwraca pacjentow odfiltrowanych po nazwisku lub PESEL. Pusty filtr = wszyscy
    /// nieusunieci. Wyniki sa posortowane po nazwisku, potem imieniu.
    /// </summary>
    Task<IReadOnlyList<PatientDto>> SearchAsync(
        string? query,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<PatientDto?> GetByIdAsync(
        int id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<PatientDto?> GetByPeselAsync(
        string pesel,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    Task<PatientDto> CreateAsync(PatientFormDto form, CancellationToken cancellationToken = default);

    Task<PatientDto?> UpdateAsync(int id, PatientFormDto form, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft delete. Ustawia <c>IsDeleted = true</c> i <c>DeletedAt</c>. Zwraca <c>false</c>, gdy pacjenta nie ma.
    /// </summary>
    Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Anonimizacja RODO. Nadpisuje dane osobowe pacjenta wartosciami zastepczymi
    /// i ustawia <c>AnonymizedAt</c>. Idempotentna — powolna ponownie nie zmienia juz zapisanej daty.
    /// </summary>
    Task<bool> AnonymizeAsync(int id, CancellationToken cancellationToken = default);
}
