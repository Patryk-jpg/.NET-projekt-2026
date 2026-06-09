using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.Models;

public class PrescribedMedication
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Powiazana procedura jest wymagana.")]
    public int ProcedurePerformedId { get; set; }

    [Required(ErrorMessage = "Wybor leku jest wymagany.")]
    public int MedicationId { get; set; }

    [Required(ErrorMessage = "Dawkowanie jest wymagane.")]
    [StringLength(100, ErrorMessage = "Dawkowanie moze miec maksymalnie 100 znakow.")]
    public string Dosage { get; set; } = string.Empty;

    [Range(1, 1_000, ErrorMessage = "Ilosc musi byc dodatnia.")]
    public int Quantity { get; set; }

    public ProcedurePerformed ProcedurePerformed { get; set; } = null!;
    public Medication Medication { get; set; } = null!;
}
