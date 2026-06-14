using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class CostReportServiceTests
{
    private static (CostReportService service, ApplicationDbContext seedDb) BuildSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-cost-report-{Guid.NewGuid()}")
            .Options;
        var seed = new ApplicationDbContext(options);
        var factory = new TestDbContextFactory(options);
        var service = new CostReportService(factory);
        return (service, seed);
    }

    [Fact]
    public async Task GenerateAsync_LiczyKosztyProcedurILekow()
    {
        // US-12: laczny koszt wizyty sklada sie z kosztu procedur oraz lekow z recept.
        var (service, seed) = BuildSut();
        SeedBaseData(seed);
        await seed.SaveChangesAsync();

        var report = await service.GenerateAsync(new CostReportFilterDto { Year = 2026, Month = 1 });

        Assert.Single(report.Rows);
        Assert.Equal(300m, report.TotalProcedureCost);
        Assert.Equal(25m, report.TotalMedicationCost);
        Assert.Equal(325m, report.TotalCost);
    }

    [Fact]
    public async Task GenerateAsync_FiltrujePoPacjencieLekarzuIMiesiacu()
    {
        // Filtry raportu musza dzialac razem, bo admin moze analizowac wybrany miesiac dla konkretnej relacji pacjent-lekarz.
        var (service, seed) = BuildSut();
        SeedBaseData(seed);
        seed.Patients.Add(SamplePatient(2, "Anna", "Zielinska", "90010100002"));
        seed.Visits.Add(SampleVisit(2, patientId: 2, doctorId: 1, new DateTime(2026, 2, 10, 9, 0, 0)));
        seed.ProceduresPerformed.Add(new ProcedurePerformed
        {
            Id = 3,
            VisitId = 2,
            Description = "Kontrola",
            ServiceCost = 90m
        });
        await seed.SaveChangesAsync();

        var report = await service.GenerateAsync(new CostReportFilterDto
        {
            PatientId = 1,
            DoctorId = 1,
            Year = 2026,
            Month = 1
        });

        Assert.Single(report.Rows);
        Assert.Equal(1, report.Rows[0].PatientId);
        Assert.Equal(new DateTime(2026, 1, 15).Month, report.Rows[0].ScheduledAt.Month);
    }

    [Fact]
    public async Task GenerateAsync_MiesiacBezRoku_RzucaWyjatek()
    {
        // Sam miesiac bez roku jest niejednoznaczny, wiec serwis wymaga podania roku.
        var (service, _) = BuildSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GenerateAsync(new CostReportFilterDto { Month = 1 }));

        Assert.Contains("roku", ex.Message);
    }

    private static void SeedBaseData(ApplicationDbContext seed)
    {
        seed.Patients.Add(SamplePatient(1, "Jan", "Kowalski", "90010100001"));
        seed.Doctors.Add(new Doctor
        {
            Id = 1,
            FirstName = "Anna",
            LastName = "Nowak",
            Specialization = "Internista",
            UserId = "doctor-user-1"
        });
        seed.Visits.Add(SampleVisit(1, patientId: 1, doctorId: 1, new DateTime(2026, 1, 15, 10, 0, 0)));
        seed.ProceduresPerformed.AddRange(
            new ProcedurePerformed
            {
                Id = 1,
                VisitId = 1,
                Description = "Konsultacja",
                ServiceCost = 150m
            },
            new ProcedurePerformed
            {
                Id = 2,
                VisitId = 1,
                Description = "USG",
                ServiceCost = 150m
            });
        seed.Medications.Add(new Medication
        {
            Id = 1,
            Name = "Paracetamol",
            UnitPrice = 12.50m
        });
        seed.PrescribedMedications.Add(new PrescribedMedication
        {
            Id = 1,
            ProcedurePerformedId = 1,
            MedicationId = 1,
            Dosage = "1 tabletka",
            Quantity = 2
        });
    }

    private static Patient SamplePatient(int id, string firstName, string lastName, string pesel) => new()
    {
        Id = id,
        FirstName = firstName,
        LastName = lastName,
        Pesel = pesel,
        InsuranceNumber = $"NFZ-{id:D3}",
        DateOfBirth = new DateTime(1990, 1, 1),
        Phone = "500100200",
        Email = $"patient{id}@example.com",
        CreatedAt = DateTime.UtcNow
    };

    private static Visit SampleVisit(int id, int patientId, int doctorId, DateTime scheduledAt) => new()
    {
        Id = id,
        PatientId = patientId,
        DoctorId = doctorId,
        ScheduledAt = scheduledAt,
        Status = VisitStatus.Completed,
        CreatedAt = DateTime.UtcNow
    };
}
