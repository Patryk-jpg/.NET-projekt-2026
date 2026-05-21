using ClinicManager.Core.Enums;

namespace ClinicManager.Core.Models;

public class Visit
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Planned;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ICollection<ProcedurePerformed> Procedures { get; set; } = new List<ProcedurePerformed>();
    public ICollection<ClinicalNote> Notes { get; set; } = new List<ClinicalNote>();
}