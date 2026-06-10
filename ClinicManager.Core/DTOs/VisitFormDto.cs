using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.DTOs;

/// <summary>
/// DTO formularza tworzenia/edycji wizyty. Walidacja po stronie klienta (DataAnnotationsValidator)
/// sprawdza obowiazkowosc pol; walidacja domenowa (data w przeszlosci, status do zmiany,
/// istnienie pacjenta i lekarza) jest w <c>VisitService</c>.
/// </summary>
public class VisitFormDto
{
    [Required(ErrorMessage = "Pacjent jest wymagany.")]
    [Range(1, int.MaxValue, ErrorMessage = "Wybierz pacjenta z listy.")]
    public int PatientId { get; set; }

    [Required(ErrorMessage = "Lekarz jest wymagany.")]
    [Range(1, int.MaxValue, ErrorMessage = "Wybierz lekarza z listy.")]
    public int DoctorId { get; set; }

    [Required(ErrorMessage = "Termin wizyty jest wymagany.")]
    public DateTime ScheduledAt { get; set; } = DateTime.Now.Date.AddDays(1).AddHours(9);
}
