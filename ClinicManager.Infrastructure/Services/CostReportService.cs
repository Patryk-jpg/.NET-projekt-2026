using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Infrastructure.Services;

public class CostReportService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : ICostReportService
{
    public async Task<CostReportDto> GenerateAsync(
        CostReportFilterDto filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        Validate(filter);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Visits
            .AsNoTracking()
            .Include(visit => visit.Patient)
            .Include(visit => visit.Doctor)
            .Include(visit => visit.Procedures)
                .ThenInclude(procedure => procedure.PrescribedMedications)
                .ThenInclude(prescription => prescription.Medication)
            .AsQueryable();

        if (filter.PatientId is not null)
        {
            query = query.Where(visit => visit.PatientId == filter.PatientId.Value);
        }

        if (filter.DoctorId is not null)
        {
            query = query.Where(visit => visit.DoctorId == filter.DoctorId.Value);
        }

        if (filter.Year is not null)
        {
            query = query.Where(visit => visit.ScheduledAt.Year == filter.Year.Value);
        }

        if (filter.Month is not null)
        {
            query = query.Where(visit => visit.ScheduledAt.Month == filter.Month.Value);
        }

        var visits = await query
            .OrderByDescending(visit => visit.ScheduledAt)
            .ToListAsync(cancellationToken);

        var rows = visits.Select(visit =>
        {
            var procedureCost = visit.Procedures.Sum(procedure => procedure.ServiceCost);
            var medicationCost = visit.Procedures
                .SelectMany(procedure => procedure.PrescribedMedications)
                .Sum(prescription => prescription.Medication.UnitPrice * prescription.Quantity);

            return new CostReportRowDto(
                visit.Id,
                visit.ScheduledAt,
                visit.Status,
                visit.PatientId,
                $"{visit.Patient.FirstName} {visit.Patient.LastName}",
                visit.Patient.Pesel,
                visit.DoctorId,
                $"{visit.Doctor.FirstName} {visit.Doctor.LastName}",
                visit.Doctor.Specialization,
                procedureCost,
                medicationCost,
                procedureCost + medicationCost);
        }).ToList();

        return new CostReportDto(
            CloneFilter(filter),
            rows,
            rows.Sum(row => row.ProcedureCost),
            rows.Sum(row => row.MedicationCost),
            rows.Sum(row => row.TotalCost));
    }

    private static void Validate(CostReportFilterDto filter)
    {
        if (filter.Month is not null && filter.Year is null)
        {
            throw new InvalidOperationException("Filtr miesiaca wymaga wybrania roku.");
        }

        if (filter.Month is < 1 or > 12)
        {
            throw new InvalidOperationException("Miesiac raportu musi byc z przedzialu 1-12.");
        }

        if (filter.Year is < 2000 or > 2100)
        {
            throw new InvalidOperationException("Rok raportu musi byc z przedzialu 2000-2100.");
        }
    }

    private static CostReportFilterDto CloneFilter(CostReportFilterDto filter) => new()
    {
        PatientId = filter.PatientId,
        DoctorId = filter.DoctorId,
        Year = filter.Year,
        Month = filter.Month
    };
}
