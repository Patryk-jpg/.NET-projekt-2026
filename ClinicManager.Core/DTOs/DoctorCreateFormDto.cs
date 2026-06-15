using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.DTOs;

public class DoctorCreateFormDto
{
    [Required(ErrorMessage = "Imie lekarza jest wymagane.")]
    [StringLength(100, ErrorMessage = "Imie moze miec maksymalnie 100 znakow.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko lekarza jest wymagane.")]
    [StringLength(100, ErrorMessage = "Nazwisko moze miec maksymalnie 100 znakow.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Specjalizacja jest wymagana.")]
    [StringLength(100, ErrorMessage = "Specjalizacja moze miec maksymalnie 100 znakow.")]
    public string Specialization { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email konta lekarza jest wymagany.")]
    [EmailAddress(ErrorMessage = "Email ma niepoprawny format.")]
    [StringLength(256, ErrorMessage = "Email moze miec maksymalnie 256 znakow.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Haslo startowe jest wymagane.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Haslo musi miec od 6 do 100 znakow.")]
    public string Password { get; set; } = "Test123!";
}
