namespace ClinicManager.Core.DTOs;

/// <summary>
/// Lekkie DTO uzywane do listy rozwijanej lekarzy w formularzu wizyty.
/// </summary>
public record DoctorOptionDto(
    int Id,
    string FullName,
    string Specialization);
