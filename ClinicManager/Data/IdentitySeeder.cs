using ClinicManager.Core.Constants;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Data;

/// <summary>
/// Tworzy podstawowe role aplikacji oraz konta testowe potrzebne do prezentacji systemu
/// (Admin, Lekarz, Rejestratorka). Uruchamiany jednorazowo przy starcie aplikacji.
/// </summary>
public static class IdentitySeeder
{
    public const string AdminEmail = "admin@clinic.local";
    public const string DoctorEmail = "lekarz@clinic.local";
    public const string ReceptionistEmail = "rejestratorka@clinic.local";
    public const string DefaultPassword = "Test123!";

    // Identyfikator zgodny z wartoscia uzyta w SeedClinicData (Doctor.UserId).
    private const string DoctorUserId = "seed-doctor-user";

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
        await EnsureUserAsync(userManager, ReceptionistEmail, Roles.Rejestratorka);
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

    private static string FormatErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}
