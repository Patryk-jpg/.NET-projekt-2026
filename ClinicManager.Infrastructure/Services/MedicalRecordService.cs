using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Infrastructure.Services;

/// <summary>
/// Implementacja kartoteki pacjenta. Pliki sa zapisywane fizycznie w
/// <c>{wwwroot}/uploads/medical-records</c>, a w bazie zostaje sciezka relatywna,
/// ktora moze byc bezposrednio podana w <c>href</c> przegladarki.
/// </summary>
public class MedicalRecordService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    MedicalRecordMapper mapper,
    string webRootPath) : IMedicalRecordService
{
    private const string UploadsSegment = "uploads";
    private const string MedicalRecordsSegment = "medical-records";

    public async Task<MedicalRecordDto> GetOrCreateForPatientAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var record = await db.MedicalRecords
            .FirstOrDefaultAsync(r => r.PatientId == patientId, cancellationToken);
        if (record is not null)
        {
            return mapper.ToDto(record);
        }

        await EnsurePatientExistsAsync(db, patientId, cancellationToken);

        record = new MedicalRecord
        {
            PatientId = patientId,
            DocumentScanUrl = null,
            CreatedAt = DateTime.UtcNow
        };
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        return mapper.ToDto(record);
    }

    public async Task<MedicalRecordDto> UploadDocumentAsync(
        int patientId,
        Stream content,
        string fileName,
        long fileSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new InvalidOperationException("Nazwa pliku jest wymagana.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) ||
            !MedicalRecordUpload.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException(
                $"Niedozwolony typ pliku '{extension}'. Dozwolone rozszerzenia: " +
                $"{string.Join(", ", MedicalRecordUpload.AllowedExtensions)}.");
        }

        if (fileSize <= 0)
        {
            throw new InvalidOperationException("Plik jest pusty.");
        }

        if (fileSize > MedicalRecordUpload.MaxFileSizeBytes)
        {
            var sizeMb = fileSize / (double)(1024 * 1024);
            var limitMb = MedicalRecordUpload.MaxFileSizeBytes / (1024 * 1024);
            throw new InvalidOperationException(
                $"Plik jest za duzy ({sizeMb:F1} MB). Maksymalny dopuszczalny rozmiar to {limitMb} MB.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var record = await db.MedicalRecords
            .FirstOrDefaultAsync(r => r.PatientId == patientId, cancellationToken);

        if (record is null)
        {
            await EnsurePatientExistsAsync(db, patientId, cancellationToken);
            record = new MedicalRecord
            {
                PatientId = patientId,
                CreatedAt = DateTime.UtcNow
            };
            db.MedicalRecords.Add(record);
        }

        var folder = GetUploadsAbsoluteFolder();
        Directory.CreateDirectory(folder);

        var storedName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(folder, storedName);

        await using (var fileStream = File.Create(fullPath))
        {
            await content.CopyToAsync(fileStream, cancellationToken);
        }

        // Po pomyslnym zapisie nowego pliku sprzatamy stary skan na dysku.
        var previousUrl = record.DocumentScanUrl;
        record.DocumentScanUrl = $"/{UploadsSegment}/{MedicalRecordsSegment}/{storedName}";

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Rollback fizyczny - zeby nie zostawic pliku w razie bledu zapisu DB.
            TryDelete(fullPath);
            throw;
        }

        if (!string.IsNullOrEmpty(previousUrl))
        {
            TryDelete(MapUrlToAbsolutePath(previousUrl));
        }

        return mapper.ToDto(record);
    }

    public async Task<bool> DeleteDocumentAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var record = await db.MedicalRecords
            .FirstOrDefaultAsync(r => r.PatientId == patientId, cancellationToken);
        if (record is null || string.IsNullOrEmpty(record.DocumentScanUrl))
        {
            return false;
        }

        var path = MapUrlToAbsolutePath(record.DocumentScanUrl);
        record.DocumentScanUrl = null;
        await db.SaveChangesAsync(cancellationToken);
        TryDelete(path);
        return true;
    }

    private static async Task EnsurePatientExistsAsync(
        ApplicationDbContext db,
        int patientId,
        CancellationToken cancellationToken)
    {
        var exists = await db.Patients
            .AnyAsync(p => p.Id == patientId && !p.IsDeleted, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException(
                $"Pacjent o Id {patientId} nie istnieje albo zostal usuniety.");
        }
    }

    private string GetUploadsAbsoluteFolder() =>
        Path.Combine(webRootPath, UploadsSegment, MedicalRecordsSegment);

    private string MapUrlToAbsolutePath(string relativeUrl)
    {
        // Z URL postaci "/uploads/medical-records/abc.pdf" tworzymy fizyczna sciezke
        // wzgledem wwwroot, niezaleznie od separatora w systemie.
        var trimmed = relativeUrl.TrimStart('/');
        var parts = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return Path.Combine(new[] { webRootPath }.Concat(parts).ToArray());
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Sprzatanie pliku nie moze blokowac operacji bazodanowych.
            // Plik zostanie ewentualnie usuniety reka administratora.
        }
    }
}
