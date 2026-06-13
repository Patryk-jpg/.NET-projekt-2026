using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.Models;

/// <summary>
/// Procedura medyczna w katalogu uslug kliniki. Sluzy jako wzorzec do wyboru
/// przy dodawaniu procedury do wizyty (<see cref="ProcedurePerformed"/>).
/// </summary>
public class Procedure
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Nazwa procedury jest wymagana.")]
    [StringLength(200, ErrorMessage = "Nazwa procedury moze miec maksymalnie 200 znakow.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Opis procedury moze miec maksymalnie 500 znakow.")]
    public string? Description { get; set; }

    [Range(0d, 1_000_000d, ErrorMessage = "Domyslny koszt procedury musi byc liczba nieujemna.")]
    public decimal DefaultCost { get; set; }
}
