using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Mappers;
using ClinicManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class VisitServiceTests
{
    private static (VisitService service, ApplicationDbContext seedDb) BuildSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-visits-{Guid.NewGuid()}")
            .Options;
        var seed = new ApplicationDbContext(options);
        var factory = new TestDbContextFactory(options);
        var service = new VisitService(factory, new VisitMapper());
        return (service, seed);
    }

    private static Patient SamplePatient(int id = 1, bool isDeleted = false) => new()
    {
        Id = id,
        FirstName = "Jan",
        LastName = "Kowalski",
        Pesel = $"9001010000{id}",
        InsuranceNumber = $"NFZ-{id:D3}",
        DateOfBirth = new DateTime(1990, 1, 1),
        Phone = "500100200",
        Email = $"jan{id}@example.com",
        IsDeleted = isDeleted,
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

    private static VisitFormDto FormFor(int patientId, int doctorId, DateTime? scheduled = null) => new()
    {
        PatientId = patientId,
        DoctorId = doctorId,
        ScheduledAt = scheduled ?? DateTime.UtcNow.AddDays(1)
    };

    [Fact]
    public async Task CreateAsync_TworzyWizyteWStatusiePlanned_IZwracaDtoZNazwami()
    {
        // Happy path: nowa wizyta jest zawsze "Zaplanowana", a DTO transportuje
        // zdenormalizowane pola pacjenta i lekarza zeby UI nie musialo dociagac danych.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();

        var dto = await service.CreateAsync(FormFor(1, 1, DateTime.UtcNow.AddDays(3)));

        Assert.True(dto.Id > 0);
        Assert.Equal(VisitStatus.Planned, dto.Status);
        Assert.Equal("Zaplanowana", VisitStatusLabels.GetLabel(dto.Status));
        Assert.Equal("Jan", dto.Patient.FirstName);
        Assert.Equal("Kowalski", dto.Patient.LastName);
        Assert.Equal("Anna", dto.Doctor.FirstName);
        Assert.Equal("Nowak", dto.Doctor.LastName);
        Assert.Equal("Internista", dto.Doctor.Specialization);
    }

    [Fact]
    public async Task CreateAsync_OdrzucaTerminWPrzeszlosci()
    {
        // Walidacja daty (US-07: walidacja daty wizyty) - termin starszy niz tolerancja serwisu
        // (15 minut wstecz) musi zostac odrzucony z czytelnym komunikatem.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(FormFor(1, 1, DateTime.UtcNow.AddDays(-1))));

        Assert.Contains("przeszlosci", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_OdrzucaSoftDeletedPacjenta()
    {
        // Wizyta dla pacjenta oznaczonego soft delete nie ma sensu - serwis musi to wychwycic
        // (zwlaszcza, ze UI ukrywa takich pacjentow w domyslnej liscie).
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1, isDeleted: true));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(FormFor(1, 1)));

        Assert.Contains("Pacjent", ex.Message);
    }

    [Fact]
    public async Task CreateAsync_OdrzucaNieistniejacegoLekarza()
    {
        // Wizyta wymaga konkretnego lekarza (FK + walidacja DataAnnotations).
        // Service idzie krok dalej i sprawdza istnienie rekordu - inaczej leciec by zaczela
        // EF-owa DbUpdateException, ktora dla UI jest mniej czytelna niz nasz wyjatek.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(FormFor(1, 999)));

        Assert.Contains("Lekarz", ex.Message);
    }

    [Fact]
    public async Task ChangeStatusAsync_PozwalaPlannedDoInProgress()
    {
        // Zaplanowana wizyta -> rozpoczecie. To pierwsza dozwolona tranzycja
        // w state machine z VisitStatusLabels.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();
        var visit = await service.CreateAsync(FormFor(1, 1));

        var updated = await service.ChangeStatusAsync(visit.Id, VisitStatus.InProgress);

        Assert.Equal(VisitStatus.InProgress, updated.Status);
        Assert.Equal("W trakcie", VisitStatusLabels.GetLabel(updated.Status));
    }

    [Fact]
    public async Task ChangeStatusAsync_BlokujeNiedozwolonePrzejscie()
    {
        // Bezposrednio z Planned -> Completed nie wolno - trzeba przejsc przez InProgress.
        // To zabezpieczenie spojnosci procesu obslugi pacjenta.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();
        var visit = await service.CreateAsync(FormFor(1, 1));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangeStatusAsync(visit.Id, VisitStatus.Completed));

        Assert.Contains("Niedozwolone przejscie", ex.Message);
    }

    [Fact]
    public async Task ChangeStatusAsync_StanFinalnyNieMaWyjscia()
    {
        // Po zakonczeniu wizyty (Completed) nie da sie nigdzie przejsc - status finalny.
        // Identycznie zachowuje sie Cancelled. To chroni przed "cofaniem" wizyt.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();
        var visit = await service.CreateAsync(FormFor(1, 1));
        await service.ChangeStatusAsync(visit.Id, VisitStatus.InProgress);
        await service.ChangeStatusAsync(visit.Id, VisitStatus.Completed);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangeStatusAsync(visit.Id, VisitStatus.Planned));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangeStatusAsync(visit.Id, VisitStatus.InProgress));
    }

    [Fact]
    public async Task ChangeStatusAsync_PozwalaAnulowacPlannedIApotemBlokujeWyjscieZCancelled()
    {
        // Anulowanie jest jedyna dozwolona alternatywa dla rozpoczecia zaplanowanej wizyty.
        // Po Cancelled status staje sie finalny, wiec dalsze przejscia musza byc odrzucone.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();
        var visit = await service.CreateAsync(FormFor(1, 1));

        var cancelled = await service.ChangeStatusAsync(visit.Id, VisitStatus.Cancelled);

        Assert.Equal(VisitStatus.Cancelled, cancelled.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ChangeStatusAsync(visit.Id, VisitStatus.InProgress));
    }

    [Fact]
    public async Task ChangeStatusAsync_ZNiezmienionymStatusem_JestIdempotentne()
    {
        // Klikniecie ponownie tego samego statusu nie moze rzucac bledu - to czesta sytuacja,
        // np. gdy rejestratorka odswiezy strone i kliknie ten sam przycisk.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();
        var visit = await service.CreateAsync(FormFor(1, 1));

        var same = await service.ChangeStatusAsync(visit.Id, VisitStatus.Planned);

        Assert.Equal(VisitStatus.Planned, same.Status);
    }

    [Fact]
    public async Task UpdateAsync_DozwolonyTylkoDlaPlanned()
    {
        // Edycja danych wizyty (pacjent, lekarz, termin) ma sens tylko, dopoki wizyta jest zaplanowana.
        // Po rozpoczeciu/zakonczeniu/anulowaniu service musi blokowac modyfikacje.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();
        var visit = await service.CreateAsync(FormFor(1, 1));
        await service.ChangeStatusAsync(visit.Id, VisitStatus.InProgress);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(visit.Id, FormFor(1, 1, DateTime.UtcNow.AddDays(2))));

        Assert.Contains("Mozna edytowac tylko wizyty", ex.Message);
    }

    [Fact]
    public async Task UpdateAsync_AktualizujePolaPlannedWizyty()
    {
        // Update zmienia pacjenta/lekarza/date w jednej operacji. Po commit nowy stan
        // musi byc widoczny w GetByIdAsync.
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(SamplePatient(1), SamplePatient(2));
        seed.Doctors.AddRange(SampleDoctor(1), SampleDoctor(2));
        await seed.SaveChangesAsync();
        var visit = await service.CreateAsync(FormFor(1, 1));

        var newDate = DateTime.UtcNow.AddDays(5);
        var updated = await service.UpdateAsync(visit.Id, FormFor(2, 2, newDate));

        Assert.Equal(2, updated.Patient.Id);
        Assert.Equal(2, updated.Doctor.Id);
        Assert.Equal(newDate.Year, updated.ScheduledAt.Year);
        Assert.Equal(newDate.Day, updated.ScheduledAt.Day);
    }

    [Fact]
    public async Task GetForPatientAsync_ZwracaTylkoTegoPacjenta_PosortowaneMalejaco()
    {
        // Lista wizyt pacjenta z DoD US-07 - musi obejmowac tylko dane wizyty tego pacjenta
        // i byc posortowana od najpozniejszej, zeby UI pokazywalo "co czeka pacjenta".
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(SamplePatient(1), SamplePatient(2));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();
        await service.CreateAsync(FormFor(1, 1, DateTime.UtcNow.AddDays(1)));
        await service.CreateAsync(FormFor(1, 1, DateTime.UtcNow.AddDays(7)));
        await service.CreateAsync(FormFor(2, 1, DateTime.UtcNow.AddDays(2)));

        var visits = await service.GetForPatientAsync(1);

        Assert.Equal(2, visits.Count);
        Assert.All(visits, v => Assert.Equal(1, v.Patient.Id));
        Assert.True(visits[0].ScheduledAt > visits[1].ScheduledAt);
    }

    [Fact]
    public async Task SearchAsync_FiltrPoStatusie_ZwracaTylkoPasujace()
    {
        // Filtr w UI (/visits?status=...) na poziomie serwisu odsiewa po VisitStatus.
        // Test sprawdza, ze w wyniku znajduja sie wylacznie wizyty w wybranym statusie.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        seed.Doctors.Add(SampleDoctor(1));
        await seed.SaveChangesAsync();
        var planned = await service.CreateAsync(FormFor(1, 1, DateTime.UtcNow.AddDays(1)));
        var toCancel = await service.CreateAsync(FormFor(1, 1, DateTime.UtcNow.AddDays(2)));
        await service.ChangeStatusAsync(toCancel.Id, VisitStatus.Cancelled);

        var plannedOnly = await service.SearchAsync(VisitStatus.Planned);
        var cancelledOnly = await service.SearchAsync(VisitStatus.Cancelled);

        Assert.Single(plannedOnly);
        Assert.Equal(planned.Id, plannedOnly[0].Id);
        Assert.Single(cancelledOnly);
        Assert.Equal(toCancel.Id, cancelledOnly[0].Id);
    }
}
