using ClinicManager.Core.Enums;

namespace ClinicManager.Core.DTOs;

/// <summary>
/// Wizyta zwracana z warstwy serwisowej. Zawiera zagniezdzone podsumowania pacjenta
/// i lekarza zamiast zdenormalizowanych stringow — UI sklada wyswietlane wartosci
/// samodzielnie (imie + nazwisko, badge statusu itp.).
/// </summary>
public record VisitDto(
    int Id,
    PatientSummaryDto Patient,
    DoctorSummaryDto Doctor,
    DateTime ScheduledAt,
    VisitStatus Status,
    DateTime CreatedAt);
