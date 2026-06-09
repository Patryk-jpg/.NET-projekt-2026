using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.Models;

public class Doctor
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Imie lekarza jest wymagane.")]
    [StringLength(100, ErrorMessage = "Imie moze miec maksymalnie 100 znakow.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko lekarza jest wymagane.")]
    [StringLength(100, ErrorMessage = "Nazwisko moze miec maksymalnie 100 znakow.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Specjalizacja jest wymagana.")]
    [StringLength(100, ErrorMessage = "Specjalizacja moze miec maksymalnie 100 znakow.")]
    public string Specialization { get; set; } = string.Empty;

    [Required(ErrorMessage = "Identyfikator konta Identity jest wymagany.")]
    [StringLength(450, ErrorMessage = "Identyfikator konta moze miec maksymalnie 450 znakow.")]
    public string UserId { get; set; } = string.Empty; // Identity link

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
