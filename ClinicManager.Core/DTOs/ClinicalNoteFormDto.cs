using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.DTOs;

public class ClinicalNoteFormDto
{
    [Required(ErrorMessage = "Tresc notatki jest wymagana.")]
    [StringLength(4000, ErrorMessage = "Tresc notatki moze miec maksymalnie 4000 znakow.")]
    public string Content { get; set; } = string.Empty;
}
