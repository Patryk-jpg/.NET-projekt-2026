using ClinicManager.Core.Constants;
using ClinicManager.Core.DTOs;
using ClinicManager.Infrastructure.Data;
using ClinicManager.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClinicManager.Tests;

public class DoctorAdminServiceTests
{
    private static async Task<(DoctorAdminService service, ApplicationDbContext db, UserManager<ApplicationUser> userManager)>
        BuildSutAsync()
    {
        var databaseName = $"doctor-admin-{Guid.NewGuid()}";
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddDbContextFactory<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<ApplicationDbContext>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        await roleManager.CreateAsync(new IdentityRole(Roles.Lekarz));
        var factory = provider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        return (new DoctorAdminService(factory, userManager), db, userManager);
    }

    [Fact]
    public async Task CreateAsync_TworzyKontoIdentityZRolaLekarzIRekordDoctor()
    {
        // Dodanie lekarza w panelu admina musi utworzyc konto logowania i rekord domenowy,
        // inaczej lekarz nie zobaczy swoich wizyt ani nie przejdzie autoryzacji rola Lekarz.
        var (service, db, userManager) = await BuildSutAsync();

        var doctor = await service.CreateAsync(new DoctorCreateFormDto
        {
            FirstName = "Adam",
            LastName = "Nowy",
            Specialization = "Ortopeda",
            Email = "adam.nowy@example.com",
            Password = "Test123!"
        });

        var user = await userManager.FindByEmailAsync("adam.nowy@example.com");
        Assert.NotNull(user);
        Assert.True(await userManager.IsInRoleAsync(user, Roles.Lekarz));
        Assert.Equal(user.Id, doctor.UserId);
        Assert.True(await db.Doctors.AnyAsync(item => item.UserId == user.Id));
    }

    [Fact]
    public async Task UpdateAsync_ZmieniaDaneLekarzaBezZmianyKontaIdentity()
    {
        // Edycja z karty US-23 dotyczy danych lekarza, a nie tozsamosci konta.
        // UserId powinien zostac stabilny, zeby historia wizyt nadal wskazywala tego samego lekarza.
        var (service, _, _) = await BuildSutAsync();
        var created = await service.CreateAsync(new DoctorCreateFormDto
        {
            FirstName = "Adam",
            LastName = "Nowy",
            Specialization = "Ortopeda",
            Email = "adam.edit@example.com",
            Password = "Test123!"
        });

        var updated = await service.UpdateAsync(created.Id, new DoctorEditFormDto
        {
            FirstName = "Adam",
            LastName = "Edytowany",
            Specialization = "Neurolog"
        });

        Assert.NotNull(updated);
        Assert.Equal(created.UserId, updated.UserId);
        Assert.Equal("Edytowany", updated.LastName);
        Assert.Equal("Neurolog", updated.Specialization);
    }

    [Fact]
    public async Task DeactivateAsync_BlokujeKontoLekarzaBezUsuwaniaRekordu()
    {
        // Usuniecie lekarza jest miekkie: blokujemy konto Identity, ale zostawiamy rekord Doctor
        // potrzebny do historycznych wizyt, raportow i notatek.
        var (service, db, userManager) = await BuildSutAsync();
        var created = await service.CreateAsync(new DoctorCreateFormDto
        {
            FirstName = "Adam",
            LastName = "Nowy",
            Specialization = "Ortopeda",
            Email = "adam.delete@example.com",
            Password = "Test123!"
        });

        var ok = await service.DeactivateAsync(created.Id);

        var user = await userManager.FindByIdAsync(created.UserId);
        Assert.True(ok);
        Assert.NotNull(user);
        Assert.True(user.LockoutEnd > DateTimeOffset.UtcNow);
        Assert.True(await db.Doctors.AnyAsync(item => item.Id == created.Id));
    }
}
