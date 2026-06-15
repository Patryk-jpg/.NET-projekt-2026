using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.Models;

public class Patient
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Imie pacjenta jest wymagane.")]
    [StringLength(100, ErrorMessage = "Imie moze miec maksymalnie 100 znakow.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko pacjenta jest wymagane.")]
    [StringLength(100, ErrorMessage = "Nazwisko moze miec maksymalnie 100 znakow.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "PESEL jest wymagany.")]
    [StringLength(11, MinimumLength = 11, ErrorMessage = "PESEL musi miec dokladnie 11 znakow.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi skladac sie z 11 cyfr.")]
    public string Pesel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Numer ubezpieczenia jest wymagany.")]
    [StringLength(50, ErrorMessage = "Numer ubezpieczenia moze miec maksymalnie 50 znakow.")]
    public string InsuranceNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data urodzenia jest wymagana.")]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Numer telefonu jest wymagany.")]
    [Phone(ErrorMessage = "Numer telefonu ma niepoprawny format.")]
    [StringLength(30, ErrorMessage = "Numer telefonu moze miec maksymalnie 30 znakow.")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres e-mail jest wymagany.")]
    [EmailAddress(ErrorMessage = "Adres e-mail jest niepoprawny.")]
    [StringLength(256, ErrorMessage = "Adres e-mail moze miec maksymalnie 256 znakow.")]
    public string Email { get; set; } = string.Empty;

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime? AnonymizedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public MedicalRecord? MedicalRecord { get; set; }
    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
