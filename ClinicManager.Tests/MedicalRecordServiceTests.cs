using ClinicManager.Core.Constants;
using ClinicManager.Core.Models;
using ClinicManager.Data;
using ClinicManager.Infrastructure.Mappers;
using ClinicManager.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class MedicalRecordServiceTests : IDisposable
{
    private readonly string _tempRoot;

    public MedicalRecordServiceTests()
    {
        // Kazdy test uzywa wlasnego katalogu, zeby uniknac kolizji rownoleglych przebiegow xUnit.
        _tempRoot = Path.Combine(Path.GetTempPath(), "clinic-mr-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
            {
                Directory.Delete(_tempRoot, recursive: true);
            }
        }
        catch
        {
            // Sprzatanie tymczasowych katalogow w testach nie moze blokowac sukcesu.
        }
    }

    private (MedicalRecordService service, ApplicationDbContext seedDb, string root) BuildSut()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-mr-tests-{Guid.NewGuid()}")
            .Options;
        var seed = new ApplicationDbContext(options);
        var factory = new TestDbContextFactory(options);
        var service = new MedicalRecordService(factory, new MedicalRecordMapper(), _tempRoot);
        return (service, seed, _tempRoot);
    }

    private static Patient SamplePatient(int id, bool isDeleted = false) => new()
    {
        Id = id,
        FirstName = "Anna",
        LastName = "Nowak",
        Pesel = $"9001010000{id}",
        InsuranceNumber = $"NFZ-{id:D3}",
        DateOfBirth = new DateTime(1990, 1, 1),
        Phone = "500100200",
        Email = $"anna{id}@example.com",
        IsDeleted = isDeleted,
        CreatedAt = DateTime.UtcNow
    };

    private static MemoryStream MakeFile(int sizeBytes)
    {
        var buffer = new byte[sizeBytes];
        new Random(42).NextBytes(buffer);
        return new MemoryStream(buffer);
    }

    [Fact]
    public async Task GetOrCreateForPatientAsync_TworzyKartoteke_GdyNieIstnieje()
    {
        // Kartoteka jest tworzona "na zadanie" - kiedy uzytkownik wejdzie na strone /record,
        // a w bazie jeszcze nie ma rekordu, service musi sam go zalozyc.
        var (service, seed, _) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();

        var dto = await service.GetOrCreateForPatientAsync(1);

        Assert.Equal(1, dto.PatientId);
        Assert.Null(dto.DocumentScanUrl);
        Assert.Single(seed.MedicalRecords.AsNoTracking());
    }

    [Fact]
    public async Task GetOrCreateForPatientAsync_RzucaWyjatek_GdyPacjentNieIstnieje()
    {
        // Nie pozwalamy zalozyc kartoteki dla nieistniejacego Id - to znak ze ktos
        // ma niewlasciwy URL/manipuluje parametrem.
        var (service, _, _) = BuildSut();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetOrCreateForPatientAsync(999));
    }

    [Fact]
    public async Task GetOrCreateForPatientAsync_RzucaWyjatek_GdyPacjentJestSoftDeleted()
    {
        // Po soft delete nie chcemy generowac nowych kartotek - traktujemy go jak nieistniejacego.
        var (service, seed, _) = BuildSut();
        seed.Patients.Add(SamplePatient(1, isDeleted: true));
        await seed.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetOrCreateForPatientAsync(1));
    }

    [Fact]
    public async Task UploadDocumentAsync_ZapisujePlikIUstawiaDocumentScanUrl()
    {
        // Sciezka happy-path: dla pacjenta bez kartoteki upload tworzy rekord,
        // zapisuje plik na dysku i ustawia URL relatywny do wwwroot.
        var (service, seed, root) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();

        await using var stream = MakeFile(2048);
        var dto = await service.UploadDocumentAsync(1, stream, "skierowanie.pdf", stream.Length);

        Assert.NotNull(dto.DocumentScanUrl);
        Assert.StartsWith("/uploads/medical-records/", dto.DocumentScanUrl);
        Assert.EndsWith(".pdf", dto.DocumentScanUrl);

        var diskPath = Path.Combine(
            root,
            dto.DocumentScanUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(diskPath));
        Assert.Equal(2048, new FileInfo(diskPath).Length);
    }

    [Fact]
    public async Task UploadDocumentAsync_OdrzucaNiedozwolonyTypPliku()
    {
        // Walidacja typu pliku (US-06: "walidacja typu pliku") - .exe nigdy nie powinien przejsc.
        var (service, seed, root) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();

        await using var stream = MakeFile(1024);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadDocumentAsync(1, stream, "wirus.exe", stream.Length));

        Assert.Contains(".exe", ex.Message);
        // Plik nie zostal zapisany na dysku.
        var subdir = Path.Combine(root, "uploads", "medical-records");
        Assert.False(Directory.Exists(subdir) && Directory.EnumerateFiles(subdir).Any());
    }

    [Fact]
    public async Task UploadDocumentAsync_OdrzucaPlikPrzekraczajacyLimit()
    {
        // Walidacja rozmiaru (US-06) - powyzej MaxFileSizeBytes leci wyjatek z czytelnym komunikatem.
        var (service, seed, _) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();

        var oversize = MedicalRecordUpload.MaxFileSizeBytes + 1;
        await using var stream = new MemoryStream(); // Strumien moze byc pusty, walidacja idzie po fileSize.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadDocumentAsync(1, stream, "ogromny.pdf", oversize));

        Assert.Contains("za duzy", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UploadDocumentAsync_OdrzucaPustyPlik()
    {
        // FileSize 0 byloby upadkiem walidacji rozmiaru "z drugiej strony" - chronimy przed
        // zapisem pustego pliku, ktory na produkcji nic nie wnosi.
        var (service, seed, _) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();

        await using var stream = new MemoryStream();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadDocumentAsync(1, stream, "pusty.pdf", 0));
    }

    [Fact]
    public async Task UploadDocumentAsync_ZastepujeStaryDokument_IUsuwaPoprzedniPlik()
    {
        // Edycja dokumentu = wgranie nowego. Stary plik na dysku musi zostac sprzatniety,
        // zeby /uploads nie puchlo nieuzywanymi danymi.
        var (service, seed, root) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();

        await using (var first = MakeFile(1024))
        {
            await service.UploadDocumentAsync(1, first, "skan-1.pdf", first.Length);
        }
        var firstDto = await service.GetOrCreateForPatientAsync(1);
        var firstUrl = firstDto.DocumentScanUrl!;
        var firstDisk = Path.Combine(
            root,
            firstUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(firstDisk));

        await using (var second = MakeFile(2048))
        {
            await service.UploadDocumentAsync(1, second, "skan-2.png", second.Length);
        }
        var afterDto = await service.GetOrCreateForPatientAsync(1);

        Assert.NotEqual(firstUrl, afterDto.DocumentScanUrl);
        Assert.EndsWith(".png", afterDto.DocumentScanUrl);
        Assert.False(File.Exists(firstDisk));

        var secondDisk = Path.Combine(
            root,
            afterDto.DocumentScanUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(secondDisk));
    }

    [Fact]
    public async Task DeleteDocumentAsync_UsuwaPlikIZerujeUrl()
    {
        // Po usunieciu DocumentScanUrl ma byc null, a fizyczny plik nie moze pozostac na dysku.
        var (service, seed, root) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();

        await using (var stream = MakeFile(1024))
        {
            await service.UploadDocumentAsync(1, stream, "skan.pdf", stream.Length);
        }
        var beforeDto = await service.GetOrCreateForPatientAsync(1);
        var diskPath = Path.Combine(
            root,
            beforeDto.DocumentScanUrl!.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(diskPath));

        var deleted = await service.DeleteDocumentAsync(1);
        var afterDto = await service.GetOrCreateForPatientAsync(1);

        Assert.True(deleted);
        Assert.Null(afterDto.DocumentScanUrl);
        Assert.False(File.Exists(diskPath));
    }

    [Fact]
    public async Task DeleteDocumentAsync_ZwracaFalse_GdyDokumentuNieMa()
    {
        // Idempotentnosc: jak nie ma czego usuwac, service nie wybucha, tylko sygnalizuje "false".
        var (service, seed, _) = BuildSut();
        seed.Patients.Add(SamplePatient(1));
        await seed.SaveChangesAsync();
        await service.GetOrCreateForPatientAsync(1);

        var deleted = await service.DeleteDocumentAsync(1);

        Assert.False(deleted);
    }
}
