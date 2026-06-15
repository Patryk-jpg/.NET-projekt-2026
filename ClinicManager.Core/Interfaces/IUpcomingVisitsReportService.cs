namespace ClinicManager.Core.Interfaces;

public interface IUpcomingVisitsReportService
{
    Task<byte[]> GeneratePdfAsync(DateOnly date, CancellationToken cancellationToken = default);
}
