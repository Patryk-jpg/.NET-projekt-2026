using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Mappers;
using ClinicManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class ProcedureServiceTests
{
    private static (ProcedureService service, ApplicationDbContext seedDb) BuildSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-procedures-{Guid.NewGuid()}")
            .Options;
        var seed = new ApplicationDbContext(options);
        var factory = new TestDbContextFactory(options);
        var service = new ProcedureService(factory, new ProcedureMapper());
        return (service, seed);
    }

    private static Patient SamplePatient(int id = 1) => new()
    {
        Id = id,
        FirstName = "Jan",
        LastName = "Kowalski",
        Pesel = $"9001010000{id}",
        InsuranceNumber = $"NFZ-{id:D3}",
        DateOfBirth = new DateTime(1990, 1, 1),
        Phone = "500100200",
        Email = $"jan{id}@example.com",
        CreatedAt = DateTime.UtcNow
    };

    private static Doctor SampleDoctor(int id = 1) => new()
    {
        Id = id,
        FirstName = "Anna",
        LastName = "Nowak",
        Specialization = "Internista",
        UserId = $"doctor-user-{id}"
    };

    private static Visit SampleVisit(int id = 1, int patientId = 1, int doctorId = 1) => new()
    {
        Id = id,
        PatientId = patientId,
        DoctorId = doctorId,
        ScheduledAt = DateTime.UtcNow.AddDays(1),
        Status = VisitStatus.InProgress,
        CreatedAt = DateTime.UtcNow
    };

    private static ProcedurePerformedFormDto ProcedureForm(string description, decimal cost) => new()
    {
        Description = description,
        ServiceCost = cost
    };

    [Fact]
    public async Task AddToVisitAsync_DodajeWieleProcedur_ILiczySume()
    {
        // US-09: jedna wizyta moze miec wiele procedur, a koszt wizyty jest suma ich kosztow.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient());
        seed.Doctors.Add(SampleDoctor());
        seed.Visits.Add(SampleVisit());
        await seed.SaveChangesAsync();

        await service.AddToVisitAsync(1, ProcedureForm("Konsultacja", 150m));
        await service.AddToVisitAsync(1, ProcedureForm("USG", 220.50m));

        var procedures = await service.GetForVisitAsync(1);
        var total = await service.GetVisitTotalCostAsync(1);

        Assert.Equal(2, procedures.Count);
        Assert.Equal(370.50m, total);
    }

    [Fact]
    public async Task AddToVisitAsync_PrzycinaOpisPrzedZapisem()
    {
        // Formularz z UI moze wyslac spacje na brzegach, ale w bazie przechowujemy czysty opis procedury.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient());
        seed.Doctors.Add(SampleDoctor());
        seed.Visits.Add(SampleVisit());
        await seed.SaveChangesAsync();

        var procedure = await service.AddToVisitAsync(1, ProcedureForm("  Badanie EKG  ", 80m));

        Assert.Equal("Badanie EKG", procedure.Description);
    }

    [Fact]
    public async Task AddToVisitAsync_OdrzucaKosztNieDodatni()
    {
        // Walidacja kosztu broni przed procedura za 0 lub wartoscia ujemna, bo wtedy suma wizyty bylaby mylaca.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient());
        seed.Doctors.Add(SampleDoctor());
        seed.Visits.Add(SampleVisit());
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddToVisitAsync(1, ProcedureForm("Konsultacja", 0m)));

        Assert.Contains("Koszt procedury", ex.Message);
    }

    [Fact]
    public async Task AddToVisitAsync_OdrzucaNieistniejacaWizyte()
    {
        // Procedura musi byc podpieta do realnej wizyty, zanim trafi do tabeli ProceduresPerformed.
        var (service, _) = BuildSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddToVisitAsync(999, ProcedureForm("Konsultacja", 150m)));

        Assert.Contains("Wizyta 999", ex.Message);
    }

    [Fact]
    public async Task GetVisitTotalCostAsync_DlaWizytyBezProcedur_ZwracaZero()
    {
        // Pusta lista procedur jest poprawnym stanem wizyty, a suma kosztow powinna wtedy wynosic 0.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient());
        seed.Doctors.Add(SampleDoctor());
        seed.Visits.Add(SampleVisit());
        await seed.SaveChangesAsync();

        var total = await service.GetVisitTotalCostAsync(1);

        Assert.Equal(0m, total);
    }
}
