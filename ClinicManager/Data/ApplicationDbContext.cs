using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ClinicManager.Core.Enums;
using ClinicManager.Core.Models;

namespace ClinicManager.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Visit> Visits => Set<Visit>();
    public DbSet<ProcedurePerformed> ProceduresPerformed => Set<ProcedurePerformed>();
    public DbSet<Medication> Medications => Set<Medication>();
    public DbSet<PrescribedMedication> PrescribedMedications => Set<PrescribedMedication>();
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Patient>(entity =>
        {
            entity.HasIndex(patient => patient.Pesel).IsUnique();

            entity.Property(patient => patient.FirstName).HasMaxLength(100);
            entity.Property(patient => patient.LastName).HasMaxLength(100);
            entity.Property(patient => patient.Pesel).HasMaxLength(11);
            entity.Property(patient => patient.InsuranceNumber).HasMaxLength(50);
            entity.Property(patient => patient.Phone).HasMaxLength(30);
            entity.Property(patient => patient.Email).HasMaxLength(256);

            entity.HasOne(patient => patient.MedicalRecord)
                .WithOne(record => record.Patient)
                .HasForeignKey<MedicalRecord>(record => record.PatientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Doctor>(entity =>
        {
            entity.HasIndex(doctor => doctor.UserId).IsUnique();

            entity.Property(doctor => doctor.FirstName).HasMaxLength(100);
            entity.Property(doctor => doctor.LastName).HasMaxLength(100);
            entity.Property(doctor => doctor.Specialization).HasMaxLength(100);
            entity.Property(doctor => doctor.UserId).HasMaxLength(450);
        });

        builder.Entity<MedicalRecord>(entity =>
        {
            entity.Property(record => record.DocumentScanUrl).HasMaxLength(500);
        });

        builder.Entity<Visit>(entity =>
        {
            entity.HasIndex(visit => new { visit.DoctorId, visit.ScheduledAt });

            entity.HasOne(visit => visit.Patient)
                .WithMany(patient => patient.Visits)
                .HasForeignKey(visit => visit.PatientId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(visit => visit.Doctor)
                .WithMany(doctor => doctor.Visits)
                .HasForeignKey(visit => visit.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ProcedurePerformed>(entity =>
        {
            entity.Property(procedure => procedure.Description).HasMaxLength(500);
            entity.Property(procedure => procedure.ServiceCost).HasPrecision(18, 2);

            entity.HasOne(procedure => procedure.Visit)
                .WithMany(visit => visit.Procedures)
                .HasForeignKey(procedure => procedure.VisitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Medication>(entity =>
        {
            entity.Property(medication => medication.Name).HasMaxLength(150);
            entity.Property(medication => medication.UnitPrice).HasPrecision(18, 2);
        });

        builder.Entity<PrescribedMedication>(entity =>
        {
            entity.Property(prescription => prescription.Dosage).HasMaxLength(100);

            entity.HasOne(prescription => prescription.ProcedurePerformed)
                .WithMany(procedure => procedure.PrescribedMedications)
                .HasForeignKey(prescription => prescription.ProcedurePerformedId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(prescription => prescription.Medication)
                .WithMany(medication => medication.Prescriptions)
                .HasForeignKey(prescription => prescription.MedicationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ClinicalNote>(entity =>
        {
            entity.Property(note => note.AuthorId).HasMaxLength(450);
            entity.Property(note => note.Content).HasMaxLength(4000);

            entity.HasOne(note => note.Visit)
                .WithMany(visit => visit.Notes)
                .HasForeignKey(note => note.VisitId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedClinicData(builder);
    }

    private static void SeedClinicData(ModelBuilder builder)
    {
        var createdAt = new DateTime(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);
        var visitAt = new DateTime(2026, 1, 2, 10, 0, 0, DateTimeKind.Utc);

        builder.Entity<Patient>().HasData(new Patient
        {
            Id = 1,
            FirstName = "Jan",
            LastName = "Kowalski",
            Pesel = "90010112345",
            InsuranceNumber = "NFZ-001",
            DateOfBirth = new DateTime(1990, 1, 1),
            Phone = "500100200",
            Email = "jan.kowalski@example.com",
            CreatedAt = createdAt,
            IsDeleted = false
        });

        builder.Entity<Doctor>().HasData(new Doctor
        {
            Id = 1,
            FirstName = "Anna",
            LastName = "Nowak",
            Specialization = "Internista",
            UserId = "seed-doctor-user"
        });

        builder.Entity<MedicalRecord>().HasData(new MedicalRecord
        {
            Id = 1,
            PatientId = 1,
            DocumentScanUrl = null,
            CreatedAt = createdAt
        });

        builder.Entity<Medication>().HasData(new Medication
        {
            Id = 1,
            Name = "Paracetamol",
            UnitPrice = 12.50m
        });

        builder.Entity<Visit>().HasData(new Visit
        {
            Id = 1,
            PatientId = 1,
            DoctorId = 1,
            ScheduledAt = visitAt,
            Status = VisitStatus.Planned,
            CreatedAt = createdAt
        });

        builder.Entity<ProcedurePerformed>().HasData(new ProcedurePerformed
        {
            Id = 1,
            VisitId = 1,
            Description = "Konsultacja lekarska",
            ServiceCost = 150m
        });

        builder.Entity<PrescribedMedication>().HasData(new PrescribedMedication
        {
            Id = 1,
            ProcedurePerformedId = 1,
            MedicationId = 1,
            Dosage = "1 tabletka co 8 godzin",
            Quantity = 1
        });

        builder.Entity<ClinicalNote>().HasData(new ClinicalNote
        {
            Id = 1,
            VisitId = 1,
            AuthorId = "seed-doctor-user",
            Content = "Pacjent testowy do demonstracji kartoteki.",
            Timestamp = createdAt
        });
    }
}
