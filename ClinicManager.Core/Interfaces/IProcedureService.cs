using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

public interface IProcedureService
{
    /// <summary>
    /// Zwraca procedury wykonane podczas wskazanej wizyty.
    /// </summary>
    Task<IReadOnlyList<ProcedurePerformedDto>> GetForVisitAsync(
        int visitId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dodaje procedure do wizyty po walidacji opisu, kosztu i istnienia wizyty.
    /// </summary>
    Task<ProcedurePerformedDto> AddToVisitAsync(
        int visitId,
        ProcedurePerformedFormDto form,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Liczy laczny koszt procedur wykonanych podczas wskazanej wizyty.
    /// </summary>
    Task<decimal> GetVisitTotalCostAsync(
        int visitId,
        CancellationToken cancellationToken = default);
}
