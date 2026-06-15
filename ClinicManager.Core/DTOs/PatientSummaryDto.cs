namespace ClinicManager.Core.DTOs;

/// <summary>
/// Minimalny podzestaw danych pacjenta osadzany w <see cref="VisitDto"/>.
/// Unika koniecznosci doladowywania pelnego <see cref="PatientDto"/> tylko po to,
/// by wyswietlic imie i PESEL na liscie wizyt.
/// </summary>
public record PatientSummaryDto(int Id, string FirstName, string LastName, string Pesel);
