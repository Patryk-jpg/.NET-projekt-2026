namespace ClinicManager.Core.Models;

public class Doctor
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty; // Identity link

    public ICollection<Visit> Visits { get; set; } = new List<Visit>();
}