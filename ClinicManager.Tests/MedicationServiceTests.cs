using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class MedicationServiceTests
{
    private static (MedicationService service, ApplicationDbContext seedDb) BuildSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-medications-{Guid.NewGuid()}")
            .Options;
        var seed = new ApplicationDbContext(options);
        var factory = new TestDbContextFactory(options);
        var service = new MedicationService(factory);
        return (service, seed);
    }

    [Fact]
    public async Task CreateAsync_ZapisujeLekZCenaJednostkowa()
    {
        // Katalog lekow przechowuje cene jednostkowa, ktora pozniej jest uzywana do kosztu recepty.
        var (service, _) = BuildSut();

        var medication = await service.CreateAsync(new MedicationFormDto
        {
            Name = "Ibuprofen",
            UnitPrice = 18.99m
        });

        Assert.True(medication.Id > 0);
        Assert.Equal("Ibuprofen", medication.Name);
        Assert.Equal(18.99m, medication.UnitPrice);
    }

    [Fact]
    public async Task CreateAsync_OdrzucaUjemnaCene()
    {
        // Cena leku nie moze byc ujemna, bo koszt recepty liczony jest jako UnitPrice * Quantity.
        var (service, _) = BuildSut();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new MedicationFormDto { Name = "Test", UnitPrice = -1m }));

        Assert.Contains("Cena leku", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_BlokujeLekUzytyWRecepcie()
    {
        // Nie usuwamy leku, ktory ma historyczne recepty, zeby nie zerwac dokumentacji wizyty.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient());
        seed.Doctors.Add(SampleDoctor());
        seed.Visits.Add(SampleVisit());
        seed.ProceduresPerformed.Add(new ProcedurePerformed
        {
            Id = 1,
            VisitId = 1,
            Description = "Konsultacja",
            ServiceCost = 150m
        });
        seed.Medications.Add(new Medication { Id = 1, Name = "Paracetamol", UnitPrice = 12.50m });
        seed.PrescribedMedications.Add(new PrescribedMedication
        {
            Id = 1,
            ProcedurePerformedId = 1,
            MedicationId = 1,
            Dosage = "1 tabletka",
            Quantity = 2
        });
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteAsync(1));

        Assert.Contains("uzyty", ex.Message);
    }

    private static Patient SamplePatient() => new()
    {
        Id = 1,
        FirstName = "Jan",
        LastName = "Kowalski",
        Pesel = "90010100001",
        InsuranceNumber = "NFZ-001",
        DateOfBirth = new DateTime(1990, 1, 1),
        Phone = "500100200",
        Email = "jan@example.com",
        CreatedAt = DateTime.UtcNow
    };

    private static Doctor SampleDoctor() => new()
    {
        Id = 1,
        FirstName = "Anna",
        LastName = "Nowak",
        Specialization = "Internista",
        UserId = "doctor-user-1"
    };

    private static Visit SampleVisit() => new()
    {
        Id = 1,
        PatientId = 1,
        DoctorId = 1,
        ScheduledAt = DateTime.UtcNow.AddDays(1),
        Status = VisitStatus.InProgress,
        CreatedAt = DateTime.UtcNow
    };
}
