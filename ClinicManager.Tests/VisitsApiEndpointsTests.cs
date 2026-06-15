using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;
using ClinicManager.Endpoints;
using ClinicManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class VisitsApiEndpointsTests
{
    [Fact]
    public async Task GetActiveVisitsAsync_ReturnsOnlyPlannedAndInProgressVisits_WithPatientAndDoctorData()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"active-visits-api-{Guid.NewGuid()}")
            .Options;

        await using var db = new ApplicationDbContext(options);
        var patient = new Patient
        {
            FirstName = "Anna",
            LastName = "Nowak",
            Pesel = "12345678901",
            InsuranceNumber = "NFZ-API",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "123456789",
            Email = "anna.api@example.com"
        };
        var doctor = new Doctor
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Specialization = "Internista",
            UserId = "api-doctor"
        };

        db.Patients.Add(patient);
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync();

        // Endpoint wydajnosciowy ma oddawac tylko wizyty, ktore nadal sa aktywne dla pracy przychodni.
        db.Visits.AddRange(
            new Visit
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ScheduledAt = new DateTime(2026, 6, 16, 9, 0, 0),
                Status = VisitStatus.Planned
            },
            new Visit
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ScheduledAt = new DateTime(2026, 6, 16, 10, 0, 0),
                Status = VisitStatus.InProgress
            },
            new Visit
            {
                PatientId = patient.Id,
                DoctorId = doctor.Id,
                ScheduledAt = new DateTime(2026, 6, 16, 11, 0, 0),
                Status = VisitStatus.Completed
            });
        await db.SaveChangesAsync();

        var result = await VisitsApiEndpoints.GetActiveVisitsAsync(db);

        Assert.Equal(2, result.Count);
        Assert.All(result, visit =>
            Assert.True(visit.Status is VisitStatus.Planned or VisitStatus.InProgress));
        Assert.Equal("Anna", result[0].PatientFirstName);
        Assert.Equal("Jan", result[0].DoctorFirstName);
    }
}
