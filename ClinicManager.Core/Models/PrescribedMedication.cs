namespace ClinicManager.Core.Models;

public class PrescribedMedication
{
    public int Id { get; set; }
    public int ProcedurePerformedId { get; set; }
    public int MedicationId { get; set; }
    public string Dosage { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public ProcedurePerformed ProcedurePerformed { get; set; } = null!;
    public Medication Medication { get; set; } = null!;
}