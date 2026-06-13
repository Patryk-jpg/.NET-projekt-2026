namespace ClinicManager.Core.DTOs;

/// <summary>
/// Lekarz jako pozycja listy wyboru (np. dropdown formularza wizyty).
/// Zawiera tylko pola potrzebne do wyswietlenia opcji — bez nawigacji ani danych wrazliwych.
/// </summary>
public record DoctorOptionDto(int Id, string FirstName, string LastName, string Specialization);
