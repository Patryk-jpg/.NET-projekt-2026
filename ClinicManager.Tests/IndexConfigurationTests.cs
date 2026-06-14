using ClinicManager.Core.Models;
using ClinicManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicManager.Tests;

public class IndexConfigurationTests
{
    [Fact]
    public void ApplicationDbContext_KonfigurujeUnikalnyIndeksNaPeselPacjenta()
    {
        // US-14: wyszukiwanie pacjenta po PESEL jest czestym scenariuszem,
        // dlatego model EF musi miec unikalny indeks na Patients.Pesel.
        using var db = CreateContext();

        var patientType = db.Model.FindEntityType(typeof(Patient));
        var index = patientType?.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual([nameof(Patient.Pesel)]));

        Assert.NotNull(index);
        Assert.True(index.IsUnique);
    }

    [Fact]
    public void ApplicationDbContext_KonfigurujeIndeksNaLekarzaIDateWizyty()
    {
        // US-14: lista wizyt lekarza po dacie korzysta z pary DoctorId + ScheduledAt,
        // wiec te kolumny powinny byc objete wspolnym indeksem.
        using var db = CreateContext();

        var visitType = db.Model.FindEntityType(typeof(Visit));
        var index = visitType?.GetIndexes()
            .SingleOrDefault(i => i.Properties.Select(p => p.Name).SequenceEqual([
                nameof(Visit.DoctorId),
                nameof(Visit.ScheduledAt)
            ]));

        Assert.NotNull(index);
        Assert.False(index.IsUnique);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"clinic-indexes-{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
