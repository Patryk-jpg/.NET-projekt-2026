using ClinicManager.Core.Enums;

namespace ClinicManager.Core.DTOs;

public record ActiveVisitApiDto(
    int Id,
    DateTime ScheduledAt,
    VisitStatus Status,
    string PatientFirstName,
    string PatientLastName,
    string PatientPesel,
    string DoctorFirstName,
    string DoctorLastName,
    string DoctorSpecialization);
