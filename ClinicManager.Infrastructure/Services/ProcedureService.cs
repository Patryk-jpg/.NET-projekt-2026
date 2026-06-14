using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Infrastructure.Services;

public class ProcedureService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    ProcedureMapper mapper) : IProcedureService
{
    public async Task<IReadOnlyList<ProcedurePerformedDto>> GetForVisitAsync(
        int visitId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureVisitExistsAsync(db, visitId, cancellationToken);

        var procedures = await db.ProceduresPerformed
            .AsNoTracking()
            .Where(procedure => procedure.VisitId == visitId)
            .OrderBy(procedure => procedure.Id)
            .ToListAsync(cancellationToken);

        return mapper.ToDtoList(procedures);
    }

    public async Task<ProcedurePerformedDto> AddToVisitAsync(
        int visitId,
        ProcedurePerformedFormDto form,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);
        Validate(form);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureVisitExistsAsync(db, visitId, cancellationToken);

        var procedure = mapper.ToEntity(form);
        procedure.VisitId = visitId;
        procedure.Description = form.Description.Trim();

        db.ProceduresPerformed.Add(procedure);
        await db.SaveChangesAsync(cancellationToken);

        return mapper.ToDto(procedure);
    }

    public async Task<decimal> GetVisitTotalCostAsync(
        int visitId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureVisitExistsAsync(db, visitId, cancellationToken);

        return await db.ProceduresPerformed
            .AsNoTracking()
            .Where(procedure => procedure.VisitId == visitId)
            .SumAsync(procedure => procedure.ServiceCost, cancellationToken);
    }

    private static void Validate(ProcedurePerformedFormDto form)
    {
        if (string.IsNullOrWhiteSpace(form.Description))
        {
            throw new InvalidOperationException("Opis procedury jest wymagany.");
        }

        if (form.Description.Trim().Length > 500)
        {
            throw new InvalidOperationException("Opis procedury moze miec maksymalnie 500 znakow.");
        }

        if (form.ServiceCost <= 0)
        {
            throw new InvalidOperationException("Koszt procedury musi byc wiekszy od 0.");
        }
    }

    private static async Task EnsureVisitExistsAsync(
        ApplicationDbContext db,
        int visitId,
        CancellationToken cancellationToken)
    {
        var exists = await db.Visits.AnyAsync(visit => visit.Id == visitId, cancellationToken);
        if (!exists)
        {
            throw new InvalidOperationException($"Wizyta {visitId} nie istnieje.");
        }
    }
}
