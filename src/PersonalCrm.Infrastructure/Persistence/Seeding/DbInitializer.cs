using Microsoft.EntityFrameworkCore;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence.Seeding;

/// <summary>
/// Idempotent first-run bootstrapper. Applies SQLite pragmas and verifies
/// the database is reachable. The first <see cref="User"/> and
/// <see cref="Instance"/> row are created by the App-layer setup wizard,
/// not here — this seeder only guarantees the schema is in a usable state
/// on first boot.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(
        AppDbContext db,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        // SQLite pragmas applied at startup. We re-apply them here because the
        // seeder may be invoked from contexts where Program.cs hasn't run
        // (tests, migration CLI).
        await db.Database.ExecuteSqlRawAsync(
            "PRAGMA journal_mode = WAL; " +
            "PRAGMA synchronous = NORMAL; " +
            "PRAGMA foreign_keys = ON; " +
            "PRAGMA busy_timeout = 5000; " +
            "PRAGMA temp_store = MEMORY;", ct);

        // No user/instance creation here — the setup wizard owns that. We
        // only ensure the DB is reachable and writable.
        if (await db.Database.CanConnectAsync(ct))
        {
            // Future home of default circle/tag suggestions ("Family", "Work",
            // "Mentors") created on a per-workspace basis during the wizard.
            await Task.CompletedTask;
        }
    }
}
