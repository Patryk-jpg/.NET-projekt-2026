namespace ClinicManager.Core.DTOs;

/// <summary>
/// Minimalny podzestaw danych lekarza osadzany w <see cref="VisitDto"/>.
/// Unika koniecznosci doladowywania pelnego profilu lekarza tylko po to,
/// by wyswietlic imie i specjalizacje na liscie wizyt.
/// </summary>
public record DoctorSummaryDto(int Id, string FirstName, string LastName, string Specialization);
