namespace ClinicManager.Core.DTOs;

public record ProcedurePerformedDto(
    int Id,
    int VisitId,
    string Description,
    decimal ServiceCost);
