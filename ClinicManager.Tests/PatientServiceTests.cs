using ClinicManager.Core.DTOs;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Mappers;
using ClinicManager.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class PatientServiceTests
{
    private static (PatientService service, ApplicationDbContext seedDb) BuildSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-tests-{Guid.NewGuid()}")
            .Options;

        var seedDb = new ApplicationDbContext(options);
        var factory = new TestDbContextFactory(options);
        var service = new PatientService(factory, new PatientMapper());
        return (service, seedDb);
    }

    private static Patient SamplePatient(
        int id,
        string firstName,
        string lastName,
        string pesel,
        bool isDeleted = false) => new()
        {
            Id = id,
            FirstName = firstName,
            LastName = lastName,
            Pesel = pesel,
            InsuranceNumber = $"NFZ-{id:D3}",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "500100200",
            Email = $"{firstName.ToLower()}.{lastName.ToLower()}@example.com",
            IsDeleted = isDeleted,
            CreatedAt = DateTime.UtcNow
        };

    [Fact]
    public async Task SearchAsync_ZwracaWszystkichAktywnych_GdyFiltrPusty()
    {
        // Sprawdza, ze brak filtra to lista wszystkich pacjentow (oprocz oznaczonych jako usunieci),
        // posortowana po nazwisku potem imieniu - zachowanie wymagane przez liste rejestracji.
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(
            SamplePatient(1, "Jan", "Kowalski", "90010100001"),
            SamplePatient(2, "Anna", "Nowak", "85020200002"),
            SamplePatient(3, "Piotr", "Adamowicz", "75030300003"),
            SamplePatient(4, "Usuniety", "Test", "65040400004", isDeleted: true));
        await seed.SaveChangesAsync();

        var result = await service.SearchAsync(query: null);

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { "Adamowicz", "Kowalski", "Nowak" }, result.Select(p => p.LastName));
    }

    [Fact]
    public async Task SearchAsync_ZnajdujePoPESEL_DokladneDopasowanie()
    {
        // Wyszukiwanie po pelnym numerze PESEL musi zwrocic dokladnie jeden wynik
        // - kluczowe dla rejestratorki, ktora identyfikuje pacjenta po PESEL.
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(
            SamplePatient(1, "Jan", "Kowalski", "90010100001"),
            SamplePatient(2, "Anna", "Nowak", "85020200002"));
        await seed.SaveChangesAsync();

        var result = await service.SearchAsync("90010100001");

        var only = Assert.Single(result);
        Assert.Equal("Kowalski", only.LastName);
        Assert.Equal("90010100001", only.Pesel);
    }

    [Fact]
    public async Task SearchAsync_ZnajdujePoFragmenciePESEL()
    {
        // Czesto rejestratorka wpisuje tylko czesc PESEL (np. data urodzenia).
        // Wyszukiwarka uzywa LIKE %x%, wiec fragment liczbowy musi pasowac do pacjentow,
        // ktorzy maja go w PESEL.
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(
            SamplePatient(1, "Jan", "Kowalski", "90010100001"),
            SamplePatient(2, "Anna", "Nowak", "90010100002"),
            SamplePatient(3, "Piotr", "Adamowicz", "75030300003"));
        await seed.SaveChangesAsync();

        var result = await service.SearchAsync("900101");

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.StartsWith("900101", p.Pesel));
    }

    [Fact]
    public async Task SearchAsync_ZnajdujePoNazwisku_FragmentPasuje()
    {
        // Wyszukiwarka po fragmencie nazwiska musi zlapac wszystkie wystapienia
        // (np. "Now" zlapie i "Nowak" i "Nowicki").
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(
            SamplePatient(1, "Anna", "Nowak", "85020200002"),
            SamplePatient(2, "Jan", "Nowicki", "70010100099"),
            SamplePatient(3, "Piotr", "Adamowicz", "75030300003"));
        await seed.SaveChangesAsync();

        var result = await service.SearchAsync("Now");

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.LastName == "Nowak");
        Assert.Contains(result, p => p.LastName == "Nowicki");
    }

    [Fact]
    public async Task SearchAsync_ZnajdujePoImieniu()
    {
        // Drugorzedne wyszukiwanie po imieniu - przydatne, gdy znamy pacjenta tylko z imienia
        // (np. "Anna"). Service szuka po LastName, FirstName i Pesel.
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(
            SamplePatient(1, "Anna", "Nowak", "85020200002"),
            SamplePatient(2, "Jan", "Kowalski", "90010100001"));
        await seed.SaveChangesAsync();

        var result = await service.SearchAsync("Anna");

        var only = Assert.Single(result);
        Assert.Equal("Anna", only.FirstName);
    }

    [Fact]
    public async Task SearchAsync_PomijaUsunietych_DomyslnieIncludeDeletedFalse()
    {
        // Soft delete (US-13): pacjent z IsDeleted = true nie moze pojawic sie na liscie
        // bez wyraznego zadania.
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(
            SamplePatient(1, "Jan", "Kowalski", "90010100001"),
            SamplePatient(2, "Usuniety", "Test", "65040400004", isDeleted: true));
        await seed.SaveChangesAsync();

        var result = await service.SearchAsync(query: null);

        var only = Assert.Single(result);
        Assert.Equal("Kowalski", only.LastName);
        Assert.False(only.IsDeleted);
    }

    [Fact]
    public async Task SearchAsync_DolaczaUsunietych_GdyIncludeDeletedTrue()
    {
        // Admin/rejestratorka moze chciec zobaczyc pelne archiwum (np. do raportow).
        // Wtedy jawnie ustawia includeDeleted = true.
        var (service, seed) = BuildSut();
        seed.Patients.AddRange(
            SamplePatient(1, "Jan", "Kowalski", "90010100001"),
            SamplePatient(2, "Usuniety", "Test", "65040400004", isDeleted: true));
        await seed.SaveChangesAsync();

        var result = await service.SearchAsync(query: null, includeDeleted: true);

        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.IsDeleted);
    }

    [Fact]
    public async Task GetByPeselAsync_ZwracaNullDlaNieistniejacego()
    {
        // Negatywny scenariusz wyszukiwania po PESEL: brak pacjenta = null,
        // bez wyjatku, bo to legalna sciezka kontroli z poziomu UI.
        var (service, _) = BuildSut();

        var result = await service.GetByPeselAsync("99999999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_DomyslnieUkrywaSoftDeletedPacjenta()
    {
        // Standardowy odczyt szczegolow tez powinien respektowac soft delete,
        // zeby bezposredni URL nie omijal ukrywania usunietych pacjentow.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1, "Usuniety", "Test", "65040400004", isDeleted: true));
        await seed.SaveChangesAsync();

        var result = await service.GetByIdAsync(1);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ZwracaSoftDeletedGdyIncludeDeletedTrue()
    {
        // Jawny tryb archiwalny pozwala serwisowi odzyskac rekord bez fizycznego usuwania go z bazy.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1, "Usuniety", "Test", "65040400004", isDeleted: true));
        await seed.SaveChangesAsync();

        var result = await service.GetByIdAsync(1, includeDeleted: true);

        Assert.NotNull(result);
        Assert.True(result.IsDeleted);
    }

    [Fact]
    public async Task GetByPeselAsync_DomyslnieUkrywaSoftDeletedPacjenta()
    {
        // Wyszukiwanie po PESEL nie powinno ujawniac usunietego pacjenta bez jawnego includeDeleted.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1, "Usuniety", "Test", "65040400004", isDeleted: true));
        await seed.SaveChangesAsync();

        var result = await service.GetByPeselAsync("65040400004");

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ZapisujePacjentaIZwracaDto()
    {
        // CRUD: nowy pacjent trafia do bazy z domyslnym IsDeleted = false oraz CreatedAt
        // ustawionym przez serwis. Zwracane DTO ma juz przypisane Id.
        var (service, seed) = BuildSut();

        var dto = await service.CreateAsync(new PatientFormDto
        {
            FirstName = "Adam",
            LastName = "Mickiewicz",
            Pesel = "55050500005",
            InsuranceNumber = "NFZ-005",
            DateOfBirth = new DateTime(1955, 5, 5),
            Phone = "500200300",
            Email = "adam@example.com"
        });

        Assert.True(dto.Id > 0);
        Assert.False(dto.IsDeleted);
        Assert.True((DateTime.UtcNow - dto.CreatedAt).TotalSeconds < 5);

        var fromDb = await seed.Patients.SingleAsync(p => p.Id == dto.Id);
        Assert.Equal("Mickiewicz", fromDb.LastName);
    }

    [Fact]
    public async Task CreateAsync_OdrzucaDuplikatPESEL()
    {
        // PESEL jest unikalny (US-04 + indeks unique w DbContext).
        // Service musi sprawdzic to przed wstawieniem i rzucic czytelny wyjatek.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1, "Jan", "Kowalski", "90010100001"));
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new PatientFormDto
            {
                FirstName = "Inny",
                LastName = "Pacjent",
                Pesel = "90010100001",
                InsuranceNumber = "NFZ-XYZ",
                DateOfBirth = new DateTime(1980, 1, 1),
                Phone = "500300400",
                Email = "x@example.com"
            }));

        Assert.Contains("PESEL", ex.Message);
    }

    [Fact]
    public async Task SoftDeleteAsync_UstawiaIsDeleted_BezUsuwaniaRekordu()
    {
        // Soft delete musi zachowac wiersz w bazie (RODO + spojnosc historyczna wizyt),
        // wystarczy ustawic flage IsDeleted. Po soft delete pacjent znika z domyslnej listy,
        // ale nadal istnieje w bazie i mozna go wyciagnac z includeDeleted = true.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1, "Jan", "Kowalski", "90010100001"));
        await seed.SaveChangesAsync();

        var ok = await service.SoftDeleteAsync(1);

        Assert.True(ok);

        var defaultList = await service.SearchAsync(query: null);
        Assert.Empty(defaultList);

        var fullList = await service.SearchAsync(query: null, includeDeleted: true);
        var only = Assert.Single(fullList);
        Assert.True(only.IsDeleted);
        Assert.Equal("Kowalski", only.LastName);
    }

    [Fact]
    public async Task UpdateAsync_BlokujeEdycjeSoftDeletedPacjenta()
    {
        // Po soft delete nie edytujemy juz danych pacjenta standardowym CRUD-em,
        // zeby nie mieszac archiwum z aktywna kartoteka.
        var (service, seed) = BuildSut();
        seed.Patients.Add(SamplePatient(1, "Usuniety", "Test", "65040400004", isDeleted: true));
        await seed.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateAsync(1, new PatientFormDto
            {
                FirstName = "Nowe",
                LastName = "Dane",
                Pesel = "65040400004",
                InsuranceNumber = "NFZ-999",
                DateOfBirth = new DateTime(1990, 1, 1),
                Phone = "500100200",
                Email = "nowe@example.com"
            }));

        Assert.Contains("usuniety", ex.Message);
    }

    [Fact]
    public async Task SoftDeleteAsync_ZwracaFalse_GdyPacjentNieIstnieje()
    {
        // UI pokazuje przycisk "Usun" tylko dla istniejacych rekordow,
        // ale serwis i tak musi byc odporny na nieistniejace Id.
        var (service, _) = BuildSut();

        var ok = await service.SoftDeleteAsync(123);

        Assert.False(ok);
    }
}
