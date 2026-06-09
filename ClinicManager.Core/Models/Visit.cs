using System.ComponentModel.DataAnnotations;
using ClinicManager.Core.Enums;

namespace ClinicManager.Core.Models;

public class Visit
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Pacjent wizyty jest wymagany.")]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Lekarz prowadzacy wizyte jest wymagany.")]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "Termin wizyty jest wymagany.")]
    public DateTime ScheduledAt { get; set; }

    [Required(ErrorMessage = "Status wizyty jest wymagany.")]
    [EnumDataType(typeof(VisitStatus), ErrorMessage = "Status wizyty ma niepoprawna wartosc.")]
    public VisitStatus Status { get; set; } = VisitStatus.Planned;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public ICollection<ProcedurePerformed> Procedures { get; set; } = new List<ProcedurePerformed>();
    public ICollection<ClinicalNote> Notes { get; set; } = new List<ClinicalNote>();
}
