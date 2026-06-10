using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Core.Models;

namespace ClinicManager.Infrastructure.Mappers;

/// <summary>
/// Mapper wizyt na DTO. Zamiast Mapperly piszemy ten mapping recznie, bo DTO zawiera pola
/// zlozone z wlasciwosci nawigacyjnych (pelne imie pacjenta/lekarza, polska etykieta statusu),
/// ktorych Mapperly nie potrafi wygenerowac w czystej formie - kod recznie tlumaczy intencje.
/// </summary>
public class VisitMapper
{
    public VisitDto ToDto(Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);
        if (visit.Patient is null || visit.Doctor is null)
        {
            throw new InvalidOperationException(
                "Visit musi miec zaladowane navigation properties Patient i Doctor (uzyj Include).");
        }

        return new VisitDto(
            visit.Id,
            visit.PatientId,
            $"{visit.Patient.FirstName} {visit.Patient.LastName}",
            visit.Patient.Pesel,
            visit.DoctorId,
            $"{visit.Doctor.FirstName} {visit.Doctor.LastName}",
            visit.Doctor.Specialization,
            visit.ScheduledAt,
            visit.Status,
            VisitStatusLabels.GetLabel(visit.Status),
            visit.CreatedAt);
    }

    public IReadOnlyList<VisitDto> ToDtoList(IEnumerable<Visit> visits) =>
        visits.Select(ToDto).ToList();

    public DoctorOptionDto ToOption(Doctor doctor) => new(
        doctor.Id,
        $"{doctor.FirstName} {doctor.LastName}",
        doctor.Specialization);
}
