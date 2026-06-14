using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Infrastructure.Services;

public class MedicationService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IMedicationService
{
    public async Task<IReadOnlyList<MedicationDto>> SearchAsync(
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var medicationsQuery = db.Medications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var normalized = query.Trim();
            medicationsQuery = medicationsQuery.Where(medication => medication.Name.Contains(normalized));
        }

        var medications = await medicationsQuery
            .OrderBy(medication => medication.Name)
            .ToListAsync(cancellationToken);

        return medications.Select(ToDto).ToList();
    }

    public async Task<IReadOnlyList<MedicationOptionDto>> GetOptionsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var medications = await db.Medications
            .AsNoTracking()
            .OrderBy(medication => medication.Name)
            .ToListAsync(cancellationToken);

        return medications
            .Select(medication => new MedicationOptionDto(medication.Id, medication.Name, medication.UnitPrice))
            .ToList();
    }

    public async Task<MedicationDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var medication = await db.Medications
            .AsNoTracking()
            .FirstOrDefaultAsync(medication => medication.Id == id, cancellationToken);

        return medication is null ? null : ToDto(medication);
    }

    public async Task<MedicationDto> CreateAsync(MedicationFormDto form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);
        Validate(form);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var medication = new Medication
        {
            Name = form.Name.Trim(),
            UnitPrice = form.UnitPrice
        };

        db.Medications.Add(medication);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(medication);
    }

    public async Task<MedicationDto?> UpdateAsync(
        int id,
        MedicationFormDto form,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);
        Validate(form);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var medication = await db.Medications.FirstOrDefaultAsync(medication => medication.Id == id, cancellationToken);
        if (medication is null)
        {
            return null;
        }

        medication.Name = form.Name.Trim();
        medication.UnitPrice = form.UnitPrice;
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(medication);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var medication = await db.Medications.FirstOrDefaultAsync(medication => medication.Id == id, cancellationToken);
        if (medication is null)
        {
            return;
        }

        var isUsed = await db.PrescribedMedications
            .AnyAsync(prescription => prescription.MedicationId == id, cancellationToken);
        if (isUsed)
        {
            throw new InvalidOperationException("Nie mozna usunac leku, ktory zostal juz uzyty w recepcie.");
        }

        db.Medications.Remove(medication);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static void Validate(MedicationFormDto form)
    {
        if (string.IsNullOrWhiteSpace(form.Name))
        {
            throw new InvalidOperationException("Nazwa leku jest wymagana.");
        }

        if (form.Name.Trim().Length > 150)
        {
            throw new InvalidOperationException("Nazwa leku moze miec maksymalnie 150 znakow.");
        }

        if (form.UnitPrice < 0)
        {
            throw new InvalidOperationException("Cena leku musi byc liczba nieujemna.");
        }
    }

    private static MedicationDto ToDto(Medication medication) =>
        new(medication.Id, medication.Name, medication.UnitPrice);
}
