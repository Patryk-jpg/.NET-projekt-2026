namespace ClinicManager.Core.DTOs;

public record CostReportDto(
    CostReportFilterDto Filter,
    IReadOnlyList<CostReportRowDto> Rows,
    decimal TotalProcedureCost,
    decimal TotalMedicationCost,
    decimal TotalCost);
