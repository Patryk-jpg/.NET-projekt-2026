using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.Models;

public class MedicalRecord
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Powiazany pacjent jest wymagany.")]
    public int PatientId { get; set; }

    [StringLength(500, ErrorMessage = "Sciezka dokumentu moze miec maksymalnie 500 znakow.")]
    public string? DocumentScanUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
}
