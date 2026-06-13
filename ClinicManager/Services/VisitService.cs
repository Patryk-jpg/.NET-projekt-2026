using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Interfaces;
using ClinicManager.Core.Models;
using ClinicManager.Data;
using ClinicManager.Infrastructure.Mappers;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Services;

/// <summary>
/// Implementacja logiki wizyt. Tak jak <c>PatientService</c> i <c>MedicalRecordService</c>
/// korzysta z <see cref="IDbContextFactory{TContext}"/>, by w Blazor Server kazda operacja miala
/// swoj krotkozyjacy <c>DbContext</c>.
/// </summary>
public class VisitService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    VisitMapper mapper) : IVisitService
{
    /// <summary>
    /// Tolerancja dla daty wizyty - pozwala zaplanowac wizyte 15 minut wstecz, zeby UI nie odrzucal
    /// wizyt utworzonych "tu i teraz" przy pierwszej minucie pozniej.
    /// </summary>
    private static readonly TimeSpan ScheduledAtPastTolerance = TimeSpan.FromMinutes(15);

    public async Task<IReadOnlyList<VisitDto>> SearchAsync(
        VisitStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(v => v.Status == status.Value);
        }

        var visits = await query
            .OrderByDescending(v => v.ScheduledAt)
            .ToListAsync(cancellationToken);
        return mapper.ToDtoList(visits);
    }

    public async Task<IReadOnlyList<VisitDto>> GetForPatientAsync(
        int patientId,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var visits = await db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => v.PatientId == patientId)
            .OrderByDescending(v => v.ScheduledAt)
            .ToListAsync(cancellationToken);
        return mapper.ToDtoList(visits);
    }

    public async Task<VisitDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var visit = await db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        return visit is null ? null : mapper.ToDto(visit);
    }

    public async Task<VisitDto> CreateAsync(VisitFormDto form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);
        ValidateScheduledAt(form.ScheduledAt);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        await EnsurePatientAndDoctorExistAsync(db, form.PatientId, form.DoctorId, cancellationToken);

        var visit = new Visit
        {
            PatientId = form.PatientId,
            DoctorId = form.DoctorId,
            ScheduledAt = form.ScheduledAt,
            Status = VisitStatus.Planned,
            CreatedAt = DateTime.UtcNow
        };
        db.Visits.Add(visit);
        await db.SaveChangesAsync(cancellationToken);

        return await ReloadAsDtoAsync(db, visit.Id, cancellationToken);
    }

    public async Task<VisitDto> UpdateAsync(int id, VisitFormDto form, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var visit = await db.Visits.FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Wizyta {id} nie istnieje.");

        if (visit.Status != VisitStatus.Planned)
        {
            // Edycja tresci wizyty (pacjent/lekarz/data) ma sens tylko, dopoki wizyta jest zaplanowana.
            // W trakcie/po zakonczeniu/anulowaniu modyfikujemy ja przez procedury, notatki i zmiany statusu.
            throw new InvalidOperationException(
                $"Mozna edytowac tylko wizyty w statusie '{VisitStatusLabels.GetLabel(VisitStatus.Planned)}'. " +
                $"Aktualny status: '{VisitStatusLabels.GetLabel(visit.Status)}'.");
        }

        ValidateScheduledAt(form.ScheduledAt);
        await EnsurePatientAndDoctorExistAsync(db, form.PatientId, form.DoctorId, cancellationToken);

        visit.PatientId = form.PatientId;
        visit.DoctorId = form.DoctorId;
        visit.ScheduledAt = form.ScheduledAt;

        await db.SaveChangesAsync(cancellationToken);
        return await ReloadAsDtoAsync(db, visit.Id, cancellationToken);
    }

    public async Task<VisitDto> ChangeStatusAsync(int id, VisitStatus newStatus, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var visit = await db.Visits.FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
            ?? throw new InvalidOperationException($"Wizyta {id} nie istnieje.");

        if (visit.Status == newStatus)
        {
            return await ReloadAsDtoAsync(db, visit.Id, cancellationToken);
        }

        if (!VisitStatusLabels.CanTransition(visit.Status, newStatus))
        {
            throw new InvalidOperationException(
                $"Niedozwolone przejscie statusu: '{VisitStatusLabels.GetLabel(visit.Status)}' -> " +
                $"'{VisitStatusLabels.GetLabel(newStatus)}'.");
        }

        visit.Status = newStatus;
        await db.SaveChangesAsync(cancellationToken);
        return await ReloadAsDtoAsync(db, visit.Id, cancellationToken);
    }

    public async Task<IReadOnlyList<VisitDto>> GetForDoctorAsync(string userId, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var doctor = await db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (doctor is null)
        {
            return Array.Empty<VisitDto>();
        }

        var query = db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .Where(v => v.DoctorId == doctor.Id);

        if (date is not null)
        {
            var day = date.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(v => v.ScheduledAt.Date == day.Date);
        }

        var visits = await query
            .OrderBy(v => v.ScheduledAt)
            .ToListAsync(cancellationToken);
        return mapper.ToDtoList(visits);
    }

    public async Task<IReadOnlyList<DoctorOptionDto>> GetDoctorOptionsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var doctors = await db.Doctors
            .AsNoTracking()
            .OrderBy(d => d.LastName)
            .ThenBy(d => d.FirstName)
            .ToListAsync(cancellationToken);
        return doctors.Select(mapper.ToOption).ToList();
    }

    private static void ValidateScheduledAt(DateTime scheduledAt)
    {
        var now = DateTime.UtcNow;
        // Forma DTO niesie DateTime w "lokalnej" postaci z UI; do porownania
        // bezpieczniej jest wykonac konwersje na UTC.
        var scheduledUtc = scheduledAt.Kind == DateTimeKind.Utc
            ? scheduledAt
            : scheduledAt.ToUniversalTime();
        if (scheduledUtc < now - ScheduledAtPastTolerance)
        {
            throw new InvalidOperationException(
                "Termin wizyty nie moze byc w przeszlosci. Wybierz date i godzine od chwili obecnej w przod.");
        }
    }

    private static async Task EnsurePatientAndDoctorExistAsync(
        ApplicationDbContext db,
        int patientId,
        int doctorId,
        CancellationToken cancellationToken)
    {
        var patientExists = await db.Patients
            .AnyAsync(p => p.Id == patientId && !p.IsDeleted, cancellationToken);
        if (!patientExists)
        {
            throw new InvalidOperationException(
                $"Pacjent o Id {patientId} nie istnieje albo zostal usuniety.");
        }

        var doctorExists = await db.Doctors.AnyAsync(d => d.Id == doctorId, cancellationToken);
        if (!doctorExists)
        {
            throw new InvalidOperationException($"Lekarz o Id {doctorId} nie istnieje.");
        }
    }

    private async Task<VisitDto> ReloadAsDtoAsync(
        ApplicationDbContext db,
        int visitId,
        CancellationToken cancellationToken)
    {
        var reloaded = await db.Visits
            .AsNoTracking()
            .Include(v => v.Patient)
            .Include(v => v.Doctor)
            .FirstAsync(v => v.Id == visitId, cancellationToken);
        return mapper.ToDto(reloaded);
    }
}
