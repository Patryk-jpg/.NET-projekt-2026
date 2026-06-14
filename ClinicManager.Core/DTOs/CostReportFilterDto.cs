using System.ComponentModel.DataAnnotations;

namespace ClinicManager.Core.DTOs;

public class CostReportFilterDto
{
    public int? PatientId { get; set; }

    public int? DoctorId { get; set; }

    [Range(2000, 2100, ErrorMessage = "Rok raportu musi byc z przedzialu 2000-2100.")]
    public int? Year { get; set; }

    [Range(1, 12, ErrorMessage = "Miesiac raportu musi byc z przedzialu 1-12.")]
    public int? Month { get; set; }
}
