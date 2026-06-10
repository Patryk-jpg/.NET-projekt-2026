namespace ClinicManager.Core.DTOs;

/// <summary>
/// Reprezentacja pacjenta zwracana z warstwy serwisowej do UI/API.
/// </summary>
public record PatientDto(
    int Id,
    string FirstName,
    string LastName,
    string Pesel,
    string InsuranceNumber,
    DateTime DateOfBirth,
    string Phone,
    string Email,
    bool IsDeleted,
    DateTime CreatedAt);
