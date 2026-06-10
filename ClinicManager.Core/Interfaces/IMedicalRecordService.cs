using ClinicManager.Core.DTOs;

namespace ClinicManager.Core.Interfaces;

/// <summary>
/// Logika kartoteki pacjenta: leniwe tworzenie, upload skanu, podmiana, usuniecie.
/// </summary>
public interface IMedicalRecordService
{
    /// <summary>
    /// Zwraca kartoteke przypisana do pacjenta. Tworzy ja, jezeli jeszcze nie istnieje.
    /// Rzuca <see cref="InvalidOperationException"/>, gdy pacjent nie istnieje lub jest soft-deleted.
    /// </summary>
    Task<MedicalRecordDto> GetOrCreateForPatientAsync(int patientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Zapisuje przyslany strumien jako nowy skan dokumentu w kartotece. Stary plik (jesli byl)
    /// jest fizycznie usuwany z dysku, a w bazie aktualizowane jest pole <c>DocumentScanUrl</c>.
    /// Waliduje rozszerzenie pliku oraz rozmiar (limit z <c>MedicalRecordUpload</c>).
    /// </summary>
    Task<MedicalRecordDto> UploadDocumentAsync(
        int patientId,
        Stream content,
        string fileName,
        long fileSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Usuwa skan dokumentu z dysku i czysci pole <c>DocumentScanUrl</c>. Sama kartoteka pozostaje.
    /// Zwraca <c>false</c>, gdy nie bylo czego usuwac.
    /// </summary>
    Task<bool> DeleteDocumentAsync(int patientId, CancellationToken cancellationToken = default);
}
