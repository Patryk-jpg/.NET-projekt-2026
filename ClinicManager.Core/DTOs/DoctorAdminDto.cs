namespace ClinicManager.Core.DTOs;

public record DoctorAdminDto(
    int Id,
    string FirstName,
    string LastName,
    string Specialization,
    string UserId,
    string Email,
    bool IsAccountActive);
