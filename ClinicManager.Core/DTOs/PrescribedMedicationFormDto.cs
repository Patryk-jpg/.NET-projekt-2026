using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.DTOs;

public class PrescribedMedicationFormDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Wybierz procedure.")]
    public int ProcedurePerformedId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Wybierz lek.")]
    public int MedicationId { get; set; }

    [Required(ErrorMessage = "Dawkowanie jest wymagane.")]
    [StringLength(100, ErrorMessage = "Dawkowanie moze miec maksymalnie 100 znakow.")]
    public string Dosage { get; set; } = string.Empty;

    [Range(1, 1000, ErrorMessage = "Ilosc musi byc dodatnia.")]
    public int Quantity { get; set; } = 1;
}
