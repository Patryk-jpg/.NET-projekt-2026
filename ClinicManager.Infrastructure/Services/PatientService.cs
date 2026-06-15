using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Infrastructure.Services;

/// <summary>
/// Implementacja CRUD pacjentow oparta na <see cref="ApplicationDbContext"/>.
/// Wyszukiwanie obejmuje nazwisko, imie i PESEL. Usuwanie jest realizowane jako soft delete
/// (kolumna <c>IsDeleted</c>).
/// </summary>
public class PatientService(IDbContextFactory<ApplicationDbContext> dbContextFactory, PatientMapper mapper)
    : IPatientService
{
    public async Task<IReadOnlyList<PatientDto>> SearchAsync(
        string? query,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Patient> patients = db.Patients.AsNoTracking();

        if (!includeDeleted)
        {
            patients = patients.Where(patient => !patient.IsDeleted);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            // Contains() jest tlumaczone na LIKE '%x%' przez EF Core (SQL Server, SQLite,
            // InMemory). Zaczynamy szukac po nazwisku, imieniu i PESEL jednoczesnie.
            var trimmed = query.Trim();
            patients = patients.Where(patient =>
                patient.LastName.Contains(trimmed) ||
                patient.FirstName.Contains(trimmed) ||
                patient.Pesel.Contains(trimmed));
        }

        var result = await patients
            .OrderBy(patient => patient.LastName)
            .ThenBy(patient => patient.FirstName)
            .ToListAsync(cancellationToken);

        return result.Select(mapper.ToDto).ToList();
    }

    public async Task<PatientDto?> GetByIdAsync(
        int id,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var patients = db.Patients.AsNoTracking();
        if (!includeDeleted)
        {
            patients = patients.Where(patient => !patient.IsDeleted);
        }

        var patient = await patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return patient is null ? null : mapper.ToDto(patient);
    }

    public async Task<PatientDto?> GetByPeselAsync(
        string pesel,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pesel))
        {
            return null;
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var patients = db.Patients.AsNoTracking();
        if (!includeDeleted)
        {
            patients = patients.Where(patient => !patient.IsDeleted);
        }

        var patient = await patients.FirstOrDefaultAsync(p => p.Pesel == pesel, cancellationToken);
        return patient is null ? null : mapper.ToDto(patient);
    }

    public async Task<PatientDto> CreateAsync(PatientFormDto form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.Patients
            .AnyAsync(p => p.Pesel == form.Pesel, cancellationToken);
        if (existing)
        {
            throw new InvalidOperationException($"Pacjent o numerze PESEL '{form.Pesel}' juz istnieje.");
        }

        var entity = mapper.ToEntity(form);
        entity.CreatedAt = DateTime.UtcNow;
        entity.IsDeleted = false;

        db.Patients.Add(entity);
        await db.SaveChangesAsync(cancellationToken);

        return mapper.ToDto(entity);
    }

    public async Task<PatientDto?> UpdateAsync(int id, PatientFormDto form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        if (entity.IsDeleted)
        {
            throw new InvalidOperationException("Nie mozna edytowac pacjenta oznaczonego jako usuniety.");
        }

        if (!string.Equals(entity.Pesel, form.Pesel, StringComparison.Ordinal))
        {
            var clash = await db.Patients
                .AnyAsync(p => p.Id != id && p.Pesel == form.Pesel, cancellationToken);
            if (clash)
            {
                throw new InvalidOperationException($"Pacjent o numerze PESEL '{form.Pesel}' juz istnieje.");
            }
        }

        mapper.Apply(form, entity);
        await db.SaveChangesAsync(cancellationToken);

        return mapper.ToDto(entity);
    }

    public async Task<bool> SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var entity = await db.Patients.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        if (entity.IsDeleted)
        {
            return true;
        }

        entity.IsDeleted = true;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
