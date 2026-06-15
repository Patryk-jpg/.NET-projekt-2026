using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Endpoints;

public static class VisitsApiEndpoints
{
    public static IEndpointRouteBuilder MapVisitsApi(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/visits/active", GetActiveVisitsAsync)
            .WithName("GetActiveVisits")
            .WithTags("Visits")
            .Produces<IReadOnlyList<ActiveVisitApiDto>>();

        return endpoints;
    }

    public static async Task<IReadOnlyList<ActiveVisitApiDto>> GetActiveVisitsAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken = default)
    {
        return await db.Visits
            .AsNoTracking()
            .Include(visit => visit.Patient)
            .Include(visit => visit.Doctor)
            .Where(visit => visit.Status == VisitStatus.Planned || visit.Status == VisitStatus.InProgress)
            .OrderBy(visit => visit.ScheduledAt)
            .Select(visit => new ActiveVisitApiDto(
                visit.Id,
                visit.ScheduledAt,
                visit.Status,
                visit.Patient.FirstName,
                visit.Patient.LastName,
                visit.Patient.Pesel,
                visit.Doctor.FirstName,
                visit.Doctor.LastName,
                visit.Doctor.Specialization))
            .ToListAsync(cancellationToken);
    }
}
