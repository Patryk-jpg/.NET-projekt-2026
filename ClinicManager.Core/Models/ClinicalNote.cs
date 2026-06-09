using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.Models;

public class ClinicalNote
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Powiazana wizyta jest wymagana.")]
    public int VisitId { get; set; }

    [Required(ErrorMessage = "Autor notatki jest wymagany.")]
    [StringLength(450, ErrorMessage = "Identyfikator autora moze miec maksymalnie 450 znakow.")]
    public string AuthorId { get; set; } = string.Empty; // Identity userId

    [Required(ErrorMessage = "Tresc notatki jest wymagana.")]
    [StringLength(4000, ErrorMessage = "Tresc notatki moze miec maksymalnie 4000 znakow.")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data wpisu notatki jest wymagana.")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Visit Visit { get; set; } = null!;
}
