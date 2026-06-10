namespace ClinicManager.Core.DTOs;

/// <summary>
/// Kartoteka pacjenta zwracana z warstwy serwisowej. Sciezka dokumentu jest URL-em
/// relatywnym wzgledem wwwroot, np. <c>/uploads/medical-records/abc.pdf</c>.
/// </summary>
public record MedicalRecordDto(
    int Id,
    int PatientId,
    string? DocumentScanUrl,
    DateTime CreatedAt);
