using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;

namespace ClinicManager.Core.Interfaces;

/// <summary>
/// Logika domenowa wizyt: wyszukiwanie po pacjencie, CRUD oraz przejscia statusow.
/// Walidacja przejsc opiera sie na <c>VisitStatusLabels.AllowedTransitions</c>.
/// </summary>
public interface IVisitService
{
    /// <summary>
    /// Pelna lista wizyt z opcjonalnym filtrem statusu, posortowana malejaco po dacie.
    /// </summary>
    Task<IReadOnlyList<VisitDto>> SearchAsync(VisitStatus? status = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wszystkie wizyty wybranego pacjenta, posortowane malejaco po dacie (najpierw nadchodzace).
    /// </summary>
    Task<IReadOnlyList<VisitDto>> GetForPatientAsync(int patientId, CancellationToken cancellationToken = default);

    Task<VisitDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tworzy wizyte ze statusem <c>Planned</c>. Waliduje istnienie pacjenta i lekarza
    /// oraz to, ze data nie jest "z przeszlosci" (15 minut tolerancji).
    /// </summary>
    Task<VisitDto> CreateAsync(VisitFormDto form, CancellationToken cancellationToken = default);

    /// <summary>
    /// Aktualizuje pacjenta/lekarza/date wizyty. Dozwolone tylko gdy aktualny status to <c>Planned</c>;
    /// dla pozostalych statusow rzuca <see cref="InvalidOperationException"/>.
    /// </summary>
    Task<VisitDto> UpdateAsync(int id, VisitFormDto form, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zmienia status zgodnie ze stanami z <c>VisitStatusLabels.AllowedTransitions</c>.
    /// Niedozwolone przejscie rzuca <see cref="InvalidOperationException"/>.
    /// </summary>
    Task<VisitDto> ChangeStatusAsync(int id, VisitStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lista lekarzy do listy rozwijanej w formularzu wizyty.
    /// </summary>
    Task<IReadOnlyList<DoctorOptionDto>> GetDoctorOptionsAsync(CancellationToken cancellationToken = default);
}
