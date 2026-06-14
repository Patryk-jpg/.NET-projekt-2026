using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

public interface ICostReportPdfService
{
    Task<byte[]> GenerateAsync(
        CostReportFilterDto filter,
        CancellationToken cancellationToken = default);
}
