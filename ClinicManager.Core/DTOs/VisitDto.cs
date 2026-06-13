using ClinicManager.Core.Enums;

namespace ClinicManager.Core.DTOs;

public record VisitDto(
    int Id,
    PatientSummaryDto Patient,
    DoctorSummaryDto Doctor,
    DateTime ScheduledAt,
    VisitStatus Status,
    DateTime CreatedAt);
