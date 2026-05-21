namespace ClinicManager.Core.Models;

public class Medication
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }

    public ICollection<PrescribedMedication> Prescriptions { get; set; } = new List<PrescribedMedication>();
}