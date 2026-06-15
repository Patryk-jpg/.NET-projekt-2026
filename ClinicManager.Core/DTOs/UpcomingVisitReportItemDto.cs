using ClinicManager.Core.Enums;

namespace ClinicManager.Core.DTOs;

public record UpcomingVisitReportItemDto(
    int VisitId,
    DateTime ScheduledAt,
    VisitStatus Status,
    string PatientName,
    string PatientPesel,
    string DoctorName,
    string DoctorSpecialization);
