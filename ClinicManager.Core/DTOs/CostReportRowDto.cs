using ClinicManager.Core.Enums;

namespace ClinicManager.Core.DTOs;

public record CostReportRowDto(
    int VisitId,
    DateTime ScheduledAt,
    VisitStatus Status,
    int PatientId,
    string PatientName,
    string PatientPesel,
    int DoctorId,
    string DoctorName,
    string DoctorSpecialization,
    decimal ProcedureCost,
    decimal MedicationCost,
    decimal TotalCost);
