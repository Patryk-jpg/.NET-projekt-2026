namespace ClinicManager.Core.DTOs;

public record ClinicalNoteDto(
    int Id,
    int VisitId,
    string AuthorId,
    string Content,
    DateTime Timestamp);
