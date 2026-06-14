using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class VisitMedicalServiceTests
{
    private static (VisitMedicalService service, ApplicationDbContext seedDb) BuildSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-visit-medical-{Guid.NewGuid()}")
            .Options;
        var seed = new ApplicationDbContext(options);
        var factory = new TestDbContextFactory(options);
        var service = new VisitMedicalService(factory);
        return (service, seed);
    }

    [Fact]
    public async Task AddPrescriptionAsync_ZapisujeLekDawkowanieIlosc_ILiczyKoszt()
    {
        // US-10: koszt recepty wynika z ceny leku oraz ilosci, a wpis jest podpiety do procedury wizyty.
        var (service, seed) = BuildSut();
        SeedVisitWithProcedureAndMedication(seed, medicationPrice: 12.50m);
        await seed.SaveChangesAsync();

        var prescription = await service.AddPrescriptionAsync(1, new PrescribedMedicationFormDto
        {
            ProcedurePerformedId = 1,
            MedicationId = 1,
            Dosage = "1 tabletka co 8 godzin",
            Quantity = 3
        });
        var total = await service.GetPrescriptionTotalCostAsync(1);

        Assert.Equal("Paracetamol", prescription.MedicationName);
        Assert.Equal("1 tabletka co 8 godzin", prescription.Dosage);
        Assert.Equal(37.50m, prescription.TotalCost);
        Assert.Equal(37.50m, total);
    }

    [Fact]
    public async Task AddPrescriptionAsync_OdrzucaProcedureSpozaWizyty()
    {
        // Recepta musi byc przypisana do procedury z tej samej wizyty, inaczej dokumentacja bylaby pomieszana.
        var (service, seed) = BuildSut();
        SeedVisitWithProcedureAndMedication(seed);
        seed.Visits.Add(SampleVisit(id: 2));
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddPrescriptionAsync(2, new PrescribedMedicationFormDto
            {
                ProcedurePerformedId = 1,
                MedicationId = 1,
                Dosage = "1 tabletka",
                Quantity = 1
            }));

        Assert.Contains("nie nalezy", ex.Message);
    }

    [Fact]
    public async Task AddNoteAsync_ZapisujeAutoraTimestampITresc()
    {
        // Notatka kliniczna musi zachowac autora i timestamp, bo jest czescia dokumentacji medycznej.
        var (service, seed) = BuildSut();
        SeedVisitWithProcedureAndMedication(seed);
        await seed.SaveChangesAsync();

        var note = await service.AddNoteAsync(
            1,
            new ClinicalNoteFormDto { Content = "Pacjent zglasza bol gardla." },
            "doctor-user-1");

        Assert.Equal("doctor-user-1", note.AuthorId);
        Assert.Equal("Pacjent zglasza bol gardla.", note.Content);
        Assert.True(note.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task AddNoteAsync_OdrzucaPustaTresc()
    {
        // Pusta notatka nie wnosi informacji klinicznej i powinna zostac zatrzymana przed zapisem.
        var (service, seed) = BuildSut();
        SeedVisitWithProcedureAndMedication(seed);
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddNoteAsync(1, new ClinicalNoteFormDto { Content = " " }, "doctor-user-1"));

        Assert.Contains("Tresc notatki", ex.Message);
    }

    private static void SeedVisitWithProcedureAndMedication(ApplicationDbContext seed, decimal medicationPrice = 10m)
    {
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
        seed.Medications.Add(new Medication
        {
            Id = 1,
            Name = "Paracetamol",
            UnitPrice = medicationPrice
        });
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

    private static Visit SampleVisit(int id = 1) => new()
    {
        Id = id,
        PatientId = 1,
        DoctorId = 1,
        ScheduledAt = DateTime.UtcNow.AddDays(1),
        Status = VisitStatus.InProgress,
        CreatedAt = DateTime.UtcNow
    };
}
