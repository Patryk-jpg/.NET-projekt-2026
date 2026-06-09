using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.Models;

public class ProcedurePerformed
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Powiazana wizyta jest wymagana.")]
    public int VisitId { get; set; }

    [Required(ErrorMessage = "Opis procedury jest wymagany.")]
    [StringLength(500, ErrorMessage = "Opis procedury moze miec maksymalnie 500 znakow.")]
    public string Description { get; set; } = string.Empty;

    [Range(0d, 1_000_000d, ErrorMessage = "Koszt procedury musi byc liczba nieujemna.")]
    public decimal ServiceCost { get; set; }

    public Visit Visit { get; set; } = null!;
    public ICollection<PrescribedMedication> PrescribedMedications { get; set; } = new List<PrescribedMedication>();
}
