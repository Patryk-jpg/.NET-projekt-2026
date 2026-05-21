namespace ClinicManager.Core.Models;

public class ClinicalNote
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public string AuthorId { get; set; } = string.Empty; // Identity userId
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Visit Visit { get; set; } = null!;
}