using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

public interface IDoctorAdminService
{
    Task<IReadOnlyList<DoctorAdminDto>> SearchAsync(string? query = null, CancellationToken cancellationToken = default);
    Task<DoctorAdminDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DoctorAdminDto> CreateAsync(DoctorCreateFormDto form, CancellationToken cancellationToken = default);
    Task<DoctorAdminDto?> UpdateAsync(int id, DoctorEditFormDto form, CancellationToken cancellationToken = default);
    Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default);
}
