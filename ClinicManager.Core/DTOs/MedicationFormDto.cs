using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.DTOs;

public class MedicationFormDto
{
    [Required(ErrorMessage = "Nazwa leku jest wymagana.")]
    [StringLength(150, ErrorMessage = "Nazwa leku moze miec maksymalnie 150 znakow.")]
    public string Name { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Cena leku musi byc liczba nieujemna.")]
    public decimal UnitPrice { get; set; }
}
