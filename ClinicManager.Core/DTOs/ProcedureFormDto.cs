using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.DTOs;

/// <summary>
/// Model formularza tworzenia i edycji procedury medycznej.
/// </summary>
public class ProcedureFormDto
{
    [Required(ErrorMessage = "Nazwa procedury jest wymagana.")]
    [StringLength(200, ErrorMessage = "Nazwa procedury moze miec maksymalnie 200 znakow.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Opis procedury moze miec maksymalnie 500 znakow.")]
    public string? Description { get; set; }

    [Range(0d, 1_000_000d, ErrorMessage = "Domyslny koszt musi byc liczba nieujemna.")]
    public decimal DefaultCost { get; set; }
}
