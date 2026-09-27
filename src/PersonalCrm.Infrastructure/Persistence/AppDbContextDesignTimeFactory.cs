using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PersonalCrm.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <see cref="AppDbContext"/>. Used by:
///   - <c>dotnet ef migrations …</c>
///   - <c>src/PersonalCrm.Migrations</c> CLI
///   - the App host's startup migration runner (so dev/prod behaviour matches)
///
/// Tells EF explicitly where the migration classes live. Without this, EF's
/// default convention looks for migrations in the DbContext's namespace plus
/// ".Migrations" — which would be
/// <c>PersonalCrm.Infrastructure.Persistence.Migrations</c>. Our actual
/// migration namespace is one level deeper,
/// <c>PersonalCrm.Infrastructure.Persistence.Migrations.SqliteMigrations</c>,
/// so the default convention silently finds zero migrations.
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connStr = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                      ?? "Data Source=./data/personal-crm.db";

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connStr, sqlite =>
            {
                sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                sqlite.MigrationsHistoryTable("__EFMigrationsHistory");
            })
            .EnableSensitiveDataLogging(false)
            .Options;

        return new AppDbContext(opts);
    }
}
