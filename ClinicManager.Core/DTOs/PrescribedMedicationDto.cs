namespace ClinicManager.Core.DTOs;

public record PrescribedMedicationDto(
    int Id,
    int ProcedurePerformedId,
    string ProcedureDescription,
    int MedicationId,
    string MedicationName,
    decimal UnitPrice,
    string Dosage,
    int Quantity,
    decimal TotalCost);
