using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Xunit;

namespace ClinicManager.Tests;

public class UpcomingVisitsReportServiceTests
{
    public UpcomingVisitsReportServiceTests()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Fact]
    public async Task GeneratePdfAsync_WhenVisitExistsForGivenDate_ReturnsPdfDocument()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"upcoming-visits-report-{Guid.NewGuid()}")
            .Options;

        await using (var db = new ApplicationDbContext(options))
        {
            var patient = new Patient
            {
                FirstName = "Anna",
                LastName = "Nowak",
                Pesel = "12345678901",
                InsuranceNumber = "INS-1",
                DateOfBirth = new DateTime(1990, 1, 1),
                Phone = "123456789",
                Email = "anna.nowak@example.com"
            };
            var doctor = new Doctor
            {
                FirstName = "Jan",
                LastName = "Kowalski",
                Specialization = "Internista",
                UserId = "doctor-user"
            };

            db.Patients.Add(patient);
            db.Doctors.Add(doctor);
            await db.SaveChangesAsync();

            // Test seeduje tylko jedna wizyte na wybrany dzien, zeby sprawdzic podstawowy happy path generowania PDF.
            db.Visits.Add(new Visit
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ScheduledAt = new DateTime(2026, 6, 16, 9, 30, 0),
                Status = VisitStatus.Planned
            });
            await db.SaveChangesAsync();
        }

        var service = new UpcomingVisitsReportService(new TestDbContextFactory(options));

        var pdf = await service.GeneratePdfAsync(new DateOnly(2026, 6, 16));

        Assert.NotEmpty(pdf);
        Assert.Equal("%PDF"u8.ToArray(), pdf[..4]);
    }
}
