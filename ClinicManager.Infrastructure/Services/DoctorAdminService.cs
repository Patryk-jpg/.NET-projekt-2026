using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Core.Interfaces;
using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Infrastructure.Services;

public class DoctorAdminService(
    IDbContextFactory<ApplicationDbContext> dbContextFactory,
    UserManager<ApplicationUser> userManager) : IDoctorAdminService
{
    public async Task<IReadOnlyList<DoctorAdminDto>> SearchAsync(
        string? query = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var doctors = db.Doctors.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var trimmed = query.Trim();
            doctors = doctors.Where(doctor =>
                doctor.FirstName.Contains(trimmed) ||
                doctor.LastName.Contains(trimmed) ||
                doctor.Specialization.Contains(trimmed));
        }

        var list = await doctors
            .OrderBy(doctor => doctor.LastName)
            .ThenBy(doctor => doctor.FirstName)
            .ToListAsync(cancellationToken);

        return await MapAsync(list);
    }

    public async Task<DoctorAdminDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var doctor = await db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        return doctor is null ? null : await MapAsync(doctor);
    }

    public async Task<DoctorAdminDto> CreateAsync(
        DoctorCreateFormDto form,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        var email = form.Email.Trim();
        var existingUser = await userManager.FindByEmailAsync(email);
        if (existingUser is not null)
        {
            throw new InvalidOperationException($"Konto o adresie '{email}' juz istnieje.");
        }

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        if (await db.Doctors.AnyAsync(
                doctor => doctor.FirstName == form.FirstName.Trim() &&
                          doctor.LastName == form.LastName.Trim() &&
                          doctor.Specialization == form.Specialization.Trim(),
                cancellationToken))
        {
            throw new InvalidOperationException("Lekarz o takich danych juz istnieje.");
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LockoutEnabled = true
        };

        var createResult = await userManager.CreateAsync(user, form.Password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Nie udalo sie utworzyc konta lekarza: {FormatErrors(createResult)}");
        }

        var roleResult = await userManager.AddToRoleAsync(user, Roles.Lekarz);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new InvalidOperationException($"Nie udalo sie przypisac roli Lekarz: {FormatErrors(roleResult)}");
        }

        var doctor = new Doctor
        {
            FirstName = form.FirstName.Trim(),
            LastName = form.LastName.Trim(),
            Specialization = form.Specialization.Trim(),
            UserId = user.Id
        };
        db.Doctors.Add(doctor);
        await db.SaveChangesAsync(cancellationToken);

        return await MapAsync(doctor);
    }

    public async Task<DoctorAdminDto?> UpdateAsync(
        int id,
        DoctorEditFormDto form,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var doctor = await db.Doctors.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (doctor is null)
        {
            return null;
        }

        doctor.FirstName = form.FirstName.Trim();
        doctor.LastName = form.LastName.Trim();
        doctor.Specialization = form.Specialization.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return await MapAsync(doctor);
    }

    public async Task<bool> DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var doctor = await db.Doctors
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (doctor is null)
        {
            return false;
        }

        var user = await userManager.FindByIdAsync(doctor.UserId);
        if (user is null)
        {
            return false;
        }

        user.LockoutEnabled = true;
        var result = await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Nie udalo sie dezaktywowac konta lekarza: {FormatErrors(result)}");
        }

        return true;
    }

    private async Task<IReadOnlyList<DoctorAdminDto>> MapAsync(IReadOnlyList<Doctor> doctors)
    {
        var result = new List<DoctorAdminDto>(doctors.Count);
        foreach (var doctor in doctors)
        {
            result.Add(await MapAsync(doctor));
        }

        return result;
    }

    private async Task<DoctorAdminDto> MapAsync(Doctor doctor)
    {
        var user = await userManager.FindByIdAsync(doctor.UserId);
        var email = user?.Email ?? string.Empty;
        var lockoutEnd = user?.LockoutEnd;
        var isActive = user is not null && (lockoutEnd is null || lockoutEnd <= DateTimeOffset.UtcNow);

        return new DoctorAdminDto(
            doctor.Id,
            doctor.FirstName,
            doctor.LastName,
            doctor.Specialization,
            doctor.UserId,
            email,
            isActive);
    }

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
