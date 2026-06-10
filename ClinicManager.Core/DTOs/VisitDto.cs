using ClinicManager.Core.Enums;

namespace ClinicManager.Core.DTOs;

/// <summary>
/// Reprezentacja wizyty wystawiana z warstwy serwisowej. Zawiera zdenormalizowane
/// pola pacjenta i lekarza, zeby UI nie musiala dodatkowo dociagac danych.
/// <c>StatusLabel</c> to gotowa, polska etykieta z <c>VisitStatusLabels</c>.
/// </summary>
public record VisitDto(
    int Id,
    int PatientId,
    string PatientFullName,
    string PatientPesel,
    int DoctorId,
    string DoctorFullName,
    string DoctorSpecialization,
    DateTime ScheduledAt,
    VisitStatus Status,
    string StatusLabel,
    DateTime CreatedAt);
