using ClinicManager.Core.Constants;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicManager.Infrastructure.Data;

/// <summary>
/// Tworzy role, konta testowe oraz dane demonstracyjne potrzebne do prezentacji systemu.
/// Uruchamiany przy starcie aplikacji po migracjach bazy danych.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminEmail = "admin@clinic.local";
    public const string DoctorEmail = "lekarz@clinic.local";
    public const string CardiologistEmail = "kardiolog@clinic.local";
    public const string PediatricianEmail = "pediatra@clinic.local";
    public const string ReceptionistEmail = "rejestratorka@clinic.local";
    public const string DefaultPassword = "Test123!";

    private const string DoctorUserId = "seed-doctor-user";
    private const string CardiologistUserId = "demo-cardiologist-user";
    private const string PediatricianUserId = "demo-pediatrician-user";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await dbContext.Database.MigrateAsync(cancellationToken);

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesAsync(roleManager);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureUserAsync(userManager, AdminEmail, Roles.Admin);
        await EnsureUserAsync(userManager, DoctorEmail, Roles.Lekarz, DoctorUserId);
        await EnsureUserAsync(userManager, CardiologistEmail, Roles.Lekarz, CardiologistUserId);
        await EnsureUserAsync(userManager, PediatricianEmail, Roles.Lekarz, PediatricianUserId);
        await EnsureUserAsync(userManager, ReceptionistEmail, Roles.Rejestratorka);

        await EnsureDemoDataAsync(dbContext, cancellationToken);
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Nie udalo sie utworzyc roli '{roleName}': {FormatErrors(result)}");
                }
            }
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string role,
        string? explicitId = null)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = explicitId ?? Guid.NewGuid().ToString(),
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, DefaultPassword);
            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Nie udalo sie utworzyc konta '{email}': {FormatErrors(createResult)}");
            }
        }
        else if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Nie udalo sie przypisac roli '{role}' do '{email}': {FormatErrors(roleResult)}");
            }
        }
    }

    private static async Task EnsureDemoDataAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var createdAt = new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc);

        var jan = await EnsurePatientAsync(db, new Patient
        {
            FirstName = "Jan",
            LastName = "Kowalski",
            Pesel = "90010112345",
            InsuranceNumber = "NFZ-001",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "500100200",
            Email = "jan.kowalski@example.com",
            CreatedAt = createdAt
        }, cancellationToken);
        var maria = await EnsurePatientAsync(db, new Patient
        {
            FirstName = "Maria",
            LastName = "Zielinska",
            Pesel = "85020212345",
            InsuranceNumber = "NFZ-002",
            DateOfBirth = new DateTime(1985, 2, 2),
            Phone = "501200300",
            Email = "maria.zielinska@example.com",
            CreatedAt = createdAt
        }, cancellationToken);
        var piotr = await EnsurePatientAsync(db, new Patient
        {
            FirstName = "Piotr",
            LastName = "Wisniewski",
            Pesel = "78030312345",
            InsuranceNumber = "NFZ-003",
            DateOfBirth = new DateTime(1978, 3, 3),
            Phone = "502300400",
            Email = "piotr.wisniewski@example.com",
            CreatedAt = createdAt
        }, cancellationToken);
        var ewa = await EnsurePatientAsync(db, new Patient
        {
            FirstName = "Ewa",
            LastName = "Wozniak",
            Pesel = "61040412345",
            InsuranceNumber = "NFZ-004",
            DateOfBirth = new DateTime(1961, 4, 4),
            Phone = "503400500",
            Email = "ewa.wozniak@example.com",
            CreatedAt = createdAt
        }, cancellationToken);

        var internist = await EnsureDoctorAsync(db, new Doctor
        {
            FirstName = "Anna",
            LastName = "Nowak",
            Specialization = "Internista",
            UserId = DoctorUserId
        }, cancellationToken);
        var cardiologist = await EnsureDoctorAsync(db, new Doctor
        {
            FirstName = "Tomasz",
            LastName = "Lewandowski",
            Specialization = "Kardiolog",
            UserId = CardiologistUserId
        }, cancellationToken);
        var pediatrician = await EnsureDoctorAsync(db, new Doctor
        {
            FirstName = "Katarzyna",
            LastName = "Mazur",
            Specialization = "Pediatra",
            UserId = PediatricianUserId
        }, cancellationToken);

        await EnsureMedicalRecordAsync(db, jan.Id, createdAt, cancellationToken);
        await EnsureMedicalRecordAsync(db, maria.Id, createdAt, cancellationToken);
        await EnsureMedicalRecordAsync(db, piotr.Id, createdAt, cancellationToken);
        await EnsureMedicalRecordAsync(db, ewa.Id, createdAt, cancellationToken);

        var paracetamol = await EnsureMedicationAsync(db, "Paracetamol", 12.50m, cancellationToken);
        var ibuprofen = await EnsureMedicationAsync(db, "Ibuprofen", 18.90m, cancellationToken);
        var amoxicillin = await EnsureMedicationAsync(db, "Amoksycylina", 35.00m, cancellationToken);

        var planned = await EnsureVisitAsync(db, jan.Id, internist.Id, new DateTime(2026, 6, 16, 9, 0, 0),
            VisitStatus.Planned, createdAt, cancellationToken);
        var inProgress = await EnsureVisitAsync(db, maria.Id, cardiologist.Id, new DateTime(2026, 6, 15, 11, 30, 0),
            VisitStatus.InProgress, createdAt, cancellationToken);
        var completed = await EnsureVisitAsync(db, piotr.Id, internist.Id, new DateTime(2026, 6, 10, 14, 0, 0),
            VisitStatus.Completed, createdAt, cancellationToken);
        var cancelled = await EnsureVisitAsync(db, ewa.Id, pediatrician.Id, new DateTime(2026, 6, 18, 8, 30, 0),
            VisitStatus.Cancelled, createdAt, cancellationToken);

        var consultation = await EnsureProcedureAsync(db, completed.Id, "Konsultacja kontrolna", 150m, cancellationToken);
        var ecg = await EnsureProcedureAsync(db, inProgress.Id, "EKG spoczynkowe", 90m, cancellationToken);
        var plannedConsultation = await EnsureProcedureAsync(db, planned.Id, "Planowana konsultacja", 120m, cancellationToken);

        await EnsurePrescriptionAsync(db, consultation.Id, paracetamol.Id, "1 tabletka co 8 godzin", 2, cancellationToken);
        await EnsurePrescriptionAsync(db, consultation.Id, ibuprofen.Id, "1 tabletka doraznie", 1, cancellationToken);
        await EnsurePrescriptionAsync(db, ecg.Id, amoxicillin.Id, "1 tabletka co 12 godzin", 1, cancellationToken);
        await EnsurePrescriptionAsync(db, plannedConsultation.Id, paracetamol.Id, "Wedlug zalecen lekarza", 1, cancellationToken);

        await EnsureClinicalNoteAsync(db, completed.Id, DoctorUserId,
            "Pacjent po konsultacji kontrolnej, zalecono obserwacje objawow.",
            new DateTime(2026, 6, 10, 14, 30, 0, DateTimeKind.Utc), cancellationToken);
        await EnsureClinicalNoteAsync(db, inProgress.Id, CardiologistUserId,
            "W trakcie diagnostyki kardiologicznej, wykonano EKG.",
            new DateTime(2026, 6, 15, 11, 45, 0, DateTimeKind.Utc), cancellationToken);
        await EnsureClinicalNoteAsync(db, cancelled.Id, PediatricianUserId,
            "Wizyta anulowana telefonicznie przez pacjenta.",
            new DateTime(2026, 6, 14, 9, 0, 0, DateTimeKind.Utc), cancellationToken);
    }

    private static async Task<Patient> EnsurePatientAsync(
        ApplicationDbContext db,
        Patient patient,
        CancellationToken cancellationToken)
    {
        var existing = await db.Patients.FirstOrDefaultAsync(p => p.Pesel == patient.Pesel, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        db.Patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken);
        return patient;
    }

    private static async Task<Doctor> EnsureDoctorAsync(
        ApplicationDbContext db,
        Doctor doctor,
        CancellationToken cancellationToken)
    {
        var existing = await db.Doctors.FirstOrDefaultAsync(d => d.UserId == doctor.UserId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        db.Doctors.Add(doctor);
        await db.SaveChangesAsync(cancellationToken);
        return doctor;
    }

    private static async Task EnsureMedicalRecordAsync(
        ApplicationDbContext db,
        int patientId,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        if (await db.MedicalRecords.AnyAsync(record => record.PatientId == patientId, cancellationToken))
        {
            return;
        }

        db.MedicalRecords.Add(new MedicalRecord
        {
            PatientId = patientId,
            CreatedAt = createdAt
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Medication> EnsureMedicationAsync(
        ApplicationDbContext db,
        string name,
        decimal unitPrice,
        CancellationToken cancellationToken)
    {
        var existing = await db.Medications.FirstOrDefaultAsync(medication => medication.Name == name, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var medication = new Medication
        {
            Name = name,
            UnitPrice = unitPrice
        };
        db.Medications.Add(medication);
        await db.SaveChangesAsync(cancellationToken);
        return medication;
    }

    private static async Task<Visit> EnsureVisitAsync(
        ApplicationDbContext db,
        int patientId,
        int doctorId,
        DateTime scheduledAt,
        VisitStatus status,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var existing = await db.Visits.FirstOrDefaultAsync(
            visit => visit.PatientId == patientId && visit.DoctorId == doctorId && visit.ScheduledAt == scheduledAt,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var visit = new Visit
        {
            PatientId = patientId,
            DoctorId = doctorId,
            ScheduledAt = scheduledAt,
            Status = status,
            CreatedAt = createdAt
        };
        db.Visits.Add(visit);
        await db.SaveChangesAsync(cancellationToken);
        return visit;
    }

    private static async Task<ProcedurePerformed> EnsureProcedureAsync(
        ApplicationDbContext db,
        int visitId,
        string description,
        decimal serviceCost,
        CancellationToken cancellationToken)
    {
        var existing = await db.ProceduresPerformed.FirstOrDefaultAsync(
            procedure => procedure.VisitId == visitId && procedure.Description == description,
            cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var procedure = new ProcedurePerformed
        {
            VisitId = visitId,
            Description = description,
            ServiceCost = serviceCost
        };
        db.ProceduresPerformed.Add(procedure);
        await db.SaveChangesAsync(cancellationToken);
        return procedure;
    }

    private static async Task EnsurePrescriptionAsync(
        ApplicationDbContext db,
        int procedureId,
        int medicationId,
        string dosage,
        int quantity,
        CancellationToken cancellationToken)
    {
        if (await db.PrescribedMedications.AnyAsync(
                prescription => prescription.ProcedurePerformedId == procedureId &&
                                prescription.MedicationId == medicationId &&
                                prescription.Dosage == dosage,
                cancellationToken))
        {
            return;
        }

        db.PrescribedMedications.Add(new PrescribedMedication
        {
            ProcedurePerformedId = procedureId,
            MedicationId = medicationId,
            Dosage = dosage,
            Quantity = quantity
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureClinicalNoteAsync(
        ApplicationDbContext db,
        int visitId,
        string authorId,
        string content,
        DateTime timestamp,
        CancellationToken cancellationToken)
    {
        if (await db.ClinicalNotes.AnyAsync(
                note => note.VisitId == visitId && note.AuthorId == authorId && note.Content == content,
                cancellationToken))
        {
            return;
        }

        db.ClinicalNotes.Add(new ClinicalNote
        {
            VisitId = visitId,
            AuthorId = authorId,
            Content = content,
            Timestamp = timestamp
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
