using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Infrastructure.Services;

public class VisitMedicalService(IDbContextFactory<ApplicationDbContext> dbContextFactory) : IVisitMedicalService
{
    public async Task<IReadOnlyList<PrescribedMedicationDto>> GetPrescriptionsForVisitAsync(
        int visitId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureVisitExistsAsync(db, visitId, cancellationToken);

        var prescriptions = await db.PrescribedMedications
            .AsNoTracking()
            .Include(prescription => prescription.Medication)
            .Include(prescription => prescription.ProcedurePerformed)
            .Where(prescription => prescription.ProcedurePerformed.VisitId == visitId)
            .OrderBy(prescription => prescription.Id)
            .ToListAsync(cancellationToken);

        return prescriptions.Select(ToDto).ToList();
    }

    public async Task<PrescribedMedicationDto> AddPrescriptionAsync(
        int visitId,
        PrescribedMedicationFormDto form,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);
        ValidatePrescription(form);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureVisitExistsAsync(db, visitId, cancellationToken);

        var procedure = await db.ProceduresPerformed
            .FirstOrDefaultAsync(
                procedure => procedure.Id == form.ProcedurePerformedId && procedure.VisitId == visitId,
                cancellationToken)
            ?? throw new InvalidOperationException("Wybrana procedura nie nalezy do tej wizyty.");

        var medication = await db.Medications
            .FirstOrDefaultAsync(medication => medication.Id == form.MedicationId, cancellationToken)
            ?? throw new InvalidOperationException("Wybrany lek nie istnieje.");

        var prescription = new PrescribedMedication
        {
            ProcedurePerformedId = procedure.Id,
            MedicationId = medication.Id,
            Dosage = form.Dosage.Trim(),
            Quantity = form.Quantity
        };

        db.PrescribedMedications.Add(prescription);
        await db.SaveChangesAsync(cancellationToken);

        prescription.ProcedurePerformed = procedure;
        prescription.Medication = medication;
        return ToDto(prescription);
    }

    public async Task<decimal> GetPrescriptionTotalCostAsync(
        int visitId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureVisitExistsAsync(db, visitId, cancellationToken);

        return await db.PrescribedMedications
            .AsNoTracking()
            .Include(prescription => prescription.Medication)
            .Include(prescription => prescription.ProcedurePerformed)
            .Where(prescription => prescription.ProcedurePerformed.VisitId == visitId)
            .SumAsync(
                prescription => prescription.Medication.UnitPrice * prescription.Quantity,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ClinicalNoteDto>> GetNotesForVisitAsync(
        int visitId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureVisitExistsAsync(db, visitId, cancellationToken);

        var notes = await db.ClinicalNotes
            .AsNoTracking()
            .Where(note => note.VisitId == visitId)
            .OrderByDescending(note => note.Timestamp)
            .ToListAsync(cancellationToken);

        return notes.Select(ToDto).ToList();
    }

    public async Task<ClinicalNoteDto> AddNoteAsync(
        int visitId,
        ClinicalNoteFormDto form,
        string authorId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);
        ValidateNote(form, authorId);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsureVisitExistsAsync(db, visitId, cancellationToken);

        var note = new ClinicalNote
        {
            VisitId = visitId,
            AuthorId = authorId.Trim(),
            Content = form.Content.Trim(),
            Timestamp = DateTime.UtcNow
        };

        db.ClinicalNotes.Add(note);
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(note);
    }

    private static void ValidatePrescription(PrescribedMedicationFormDto form)
    {
        if (form.ProcedurePerformedId <= 0)
        {
            throw new InvalidOperationException("Wybierz procedure.");
        }

        if (form.MedicationId <= 0)
        {
            throw new InvalidOperationException("Wybierz lek.");
        }

        if (string.IsNullOrWhiteSpace(form.Dosage))
        {
            throw new InvalidOperationException("Dawkowanie jest wymagane.");
        }

        if (form.Dosage.Trim().Length > 100)
        {
            throw new InvalidOperationException("Dawkowanie moze miec maksymalnie 100 znakow.");
        }

        if (form.Quantity is < 1 or > 1000)
        {
            throw new InvalidOperationException("Ilosc musi byc dodatnia i nie wieksza niz 1000.");
        }
    }

    private static void ValidateNote(ClinicalNoteFormDto form, string authorId)
    {
        if (string.IsNullOrWhiteSpace(authorId))
        {
            throw new InvalidOperationException("Autor notatki jest wymagany.");
        }

        if (string.IsNullOrWhiteSpace(form.Content))
        {
            throw new InvalidOperationException("Tresc notatki jest wymagana.");
        }

        if (form.Content.Trim().Length > 4000)
        {
            throw new InvalidOperationException("Tresc notatki moze miec maksymalnie 4000 znakow.");
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

    private static PrescribedMedicationDto ToDto(PrescribedMedication prescription) =>
        new(
            prescription.Id,
            prescription.ProcedurePerformedId,
            prescription.ProcedurePerformed.Description,
            prescription.MedicationId,
            prescription.Medication.Name,
            prescription.Medication.UnitPrice,
            prescription.Dosage,
            prescription.Quantity,
            prescription.Medication.UnitPrice * prescription.Quantity);

    private static ClinicalNoteDto ToDto(ClinicalNote note) =>
        new(note.Id, note.VisitId, note.AuthorId, note.Content, note.Timestamp);
}
