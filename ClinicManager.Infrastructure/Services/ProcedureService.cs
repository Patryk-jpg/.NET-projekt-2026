using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Infrastructure.Services;

/// <summary>
/// Implementacja katalogu procedur medycznych. Usuniecie jest twarde (brak soft delete)
/// — procedura z katalogu nie ma stanu cyklu zycia, moze byc bezpiecznie usunieta
/// jesli nie jest uzyta w zadnej wizycie.
/// </summary>
public class ProcedureService(IDbContextFactory<ApplicationDbContext> dbContextFactory, ProcedureMapper mapper)
    : IProcedureService
{
    public async Task<IReadOnlyList<ProcedureDto>> SearchAsync(string? query, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Procedure> procedures = db.Procedures.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var trimmed = query.Trim();
            procedures = procedures.Where(p => p.Name.Contains(trimmed) || (p.Description != null && p.Description.Contains(trimmed)));
        }

        var result = await procedures.OrderBy(p => p.Name).ToListAsync(cancellationToken);
        return result.Select(mapper.ToDto).ToList();
    }

    public async Task<ProcedureDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var procedure = await db.Procedures.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return procedure is null ? null : mapper.ToDto(procedure);
    }

    public async Task<ProcedureDto> CreateAsync(ProcedureFormDto form, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var procedure = mapper.ToEntity(form);
        db.Procedures.Add(procedure);
        await db.SaveChangesAsync(cancellationToken);
        return mapper.ToDto(procedure);
    }

    public async Task<ProcedureDto?> UpdateAsync(int id, ProcedureFormDto form, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var procedure = await db.Procedures.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (procedure is null) return null;
        mapper.Apply(form, procedure);
        await db.SaveChangesAsync(cancellationToken);
        return mapper.ToDto(procedure);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var procedure = await db.Procedures.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (procedure is null) return false;
        db.Procedures.Remove(procedure);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
