using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.Models;

public class Medication
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa leku jest wymagana.")]
    [StringLength(150, ErrorMessage = "Nazwa leku moze miec maksymalnie 150 znakow.")]
    public string Name { get; set; } = string.Empty;

    [Range(0d, 1_000_000d, ErrorMessage = "Cena leku musi byc liczba nieujemna.")]
    public decimal UnitPrice { get; set; }

    public ICollection<PrescribedMedication> Prescriptions { get; set; } = new List<PrescribedMedication>();
}
