namespace ClinicManager.Core.DTOs;

public record MedicationDto(
    int Id,
    string Name,
    decimal UnitPrice);
