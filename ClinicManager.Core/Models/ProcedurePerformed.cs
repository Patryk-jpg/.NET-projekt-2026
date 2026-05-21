namespace ClinicManager.Core.Models;

public class ProcedurePerformed
{
    public int Id { get; set; }
    public int VisitId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal ServiceCost { get; set; }

    public Visit Visit { get; set; } = null!;
    public ICollection<PrescribedMedication> PrescribedMedications { get; set; } = new List<PrescribedMedication>();
}