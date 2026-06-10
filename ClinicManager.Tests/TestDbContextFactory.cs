using ClinicManager.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicManager.Tests;

/// <summary>
/// Lekka implementacja <see cref="IDbContextFactory{TContext}"/> dla testow.
/// Pozwala wpiac InMemory DbContextOptions i dostarczyc swiezy DbContext na kazde wywolanie.
/// </summary>
public sealed class TestDbContextFactory(DbContextOptions<ApplicationDbContext> options)
    : IDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext() => new(options);
}
