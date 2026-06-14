using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

public interface IMedicationService
{
    Task<IReadOnlyList<MedicationDto>> SearchAsync(string? query = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MedicationOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default);

    Task<MedicationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<MedicationDto> CreateAsync(MedicationFormDto form, CancellationToken cancellationToken = default);

    Task<MedicationDto?> UpdateAsync(int id, MedicationFormDto form, CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
