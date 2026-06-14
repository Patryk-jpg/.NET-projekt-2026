namespace ClinicManager.Core.Interfaces;

public interface IVisitPdfService
{
    Task<byte[]> GenerateVisitCardAsync(int visitId, CancellationToken cancellationToken = default);
}
