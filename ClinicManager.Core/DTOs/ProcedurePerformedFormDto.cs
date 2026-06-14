using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.DTOs;

public class ProcedurePerformedFormDto
{
    [Required(ErrorMessage = "Opis procedury jest wymagany.")]
    [StringLength(500, ErrorMessage = "Opis procedury moze miec maksymalnie 500 znakow.")]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "Koszt procedury musi byc wiekszy od 0.")]
    public decimal ServiceCost { get; set; }
}
