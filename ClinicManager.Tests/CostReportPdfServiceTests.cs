using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Interfaces;
using ClinicManager.Services;
using Microsoft.Extensions.Logging.Abstractions;
using QuestPDF.Infrastructure;
using Xunit;

namespace ClinicManager.Tests;

public class CostReportPdfServiceTests
{
    public CostReportPdfServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task GenerateAsync_ZwracaPdfDlaAktualnychFiltrow()
    {
        // Eksport PDF powinien korzystac z tego samego filtra, ktory zasila widok raportu.
        var service = new CostReportPdfService(
            new FakeCostReportService(),
            NullLogger<CostReportPdfService>.Instance);

        var pdf = await service.GenerateAsync(new CostReportFilterDto { Year = 2026, Month = 1 });

        Assert.True(pdf.Length > 0);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(pdf[..4]));
    }

    private sealed class FakeCostReportService : ICostReportService
    {
        public Task<CostReportDto> GenerateAsync(
            CostReportFilterDto filter,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CostReportDto(
                filter,
                [
                    new CostReportRowDto(
                        1,
                        new DateTime(2026, 1, 15, 10, 0, 0),
                        VisitStatus.Completed,
                        1,
                        "Jan Kowalski",
                        "90010100001",
                        1,
                        "Anna Nowak",
                        "Internista",
                        150m,
                        25m,
                        175m)
                ],
                150m,
                25m,
                175m));
    }
}
