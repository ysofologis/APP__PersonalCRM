using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PersonalCrm.Infrastructure.Persistence;

namespace PersonalCrm.Tests.Integration.Infrastructure;

/// <summary>
/// Builds a <see cref="AppDbContext"/> backed by a single in-memory SQLite
/// connection. SQLite's <c>:memory:</c> is per-connection, so we hold the
/// connection open for the lifetime of the test and share it across contexts
/// via connection-string sharing. Pragmas match production (WAL is unavailable
/// for <c>:memory:</c>; we apply the others).
/// </summary>
public static class SqliteInMemoryDb
{
    public static (AppDbContext Context, SqliteConnection KeepAlive) Create()
    {
        // Open the shared in-memory connection. The "Pooling=False" and unique
        // name make sure each test gets a fresh database.
        var conn = new SqliteConnection($"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared;Pooling=False");
        conn.Open();

        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .EnableSensitiveDataLogging(false)
            .Options;

        var ctx = new AppDbContext(opts);

        // Apply the same pragmas the App layer applies on startup. WAL is
        // not available for in-memory databases, so we skip it.
        ctx.Database.ExecuteSqlRaw("PRAGMA foreign_keys = ON;");
        ctx.Database.ExecuteSqlRaw("PRAGMA synchronous = NORMAL;");

        // Apply the initial schema via the migration name shipped in the
        // Infrastructure project.
        ctx.Database.Migrate();

        return (ctx, conn);
    }
}
