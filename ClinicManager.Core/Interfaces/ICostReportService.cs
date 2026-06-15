using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

public interface ICostReportService
{
    Task<CostReportDto> GenerateAsync(
        CostReportFilterDto filter,
        CancellationToken cancellationToken = default);
}
