using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Infrastructure.Persistence.WorkspaceContext;
using PersonalCrm.Contracts.Dtos;
using PersonalCrm.App;

namespace PersonalCrm.Tests.Integration.Infrastructure;

/// <summary>
/// Boots the real Personal CRM host in-process for integration tests.
/// SQLite is wired to a unique in-memory database per factory instance so
/// tests don't share state. Auth is replaced with <see cref="TestAuthHandler"/>.
/// </summary>
public class CrmWebAppFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CrmWebAppFactory()
    {
        // Single in-memory DB shared by every DbContext the factory creates.
        _connection = new SqliteConnection($"DataSource=file:{Guid.NewGuid():N}?mode=memory&cache=shared;Pooling=False");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"]       = _connection.ConnectionString,
                ["AttachmentStore:Provider"]       = "local",
                ["AttachmentStore:Local:Root"]     = Path.Combine(Path.GetTempPath(), $"pcrm-attachments-{Guid.NewGuid():N}"),
                ["Auth:JwtSigningKey"]             = "test_signing_key_must_be_at_least_64_hex_chars_long_aaaaaaaaaaaaaaaaaaaaaaa",
                // Disable the dev-mode bootstrap so tests start with a virgin DB
                // and `POST /api/setup` behaves like the original suite assumes
                // (200 on first call, 409 on subsequent).
                ["Bootstrap:SeedDefault"]          = "false"
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // Replace the real DbContext with one bound to the shared in-memory connection.
            var toRemove = services.Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)).ToList();
            foreach (var d in toRemove) services.Remove(d);

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseSqlite(_connection)
                   .EnableSensitiveDataLogging(false));

            // Replace auth with the test handler.
            services.AddAuthentication(defaultScheme: TestAuthHandler.SchemeName)
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>Run schema migrations and seed an instance admin user + workspace.</summary>
    public async Task<(Guid UserId, Guid WorkspaceId)> SeedAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var userId = Guid.NewGuid();
        var wsId   = Guid.NewGuid();
        var now    = DateTimeOffset.UtcNow;

        db.Users.Add(new User
        {
            Id              = userId,
            Email           = $"user-{userId:N}@test.local",
            PasswordHash    = "$argon2id$v=19$m=65536,t=3,p=4$AAAAAAAAAAAAAAAAAAAAAA$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            DisplayName     = "Test User",
            IsInstanceAdmin = false,
            IsEmailVerified = true,
            CreatedAt       = now
        });

        db.Workspaces.Add(new Workspace
        {
            Id         = wsId,
            Name       = "Test Workspace",
            IsPersonal = false,
            CreatedAt  = now
        });

        db.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = wsId,
            UserId      = userId,
            Role        = WorkspaceRole.Owner,
            JoinedAt    = now
        });

        await db.SaveChangesAsync();
        return (userId, wsId);
    }

    /// <summary>Wipe user/workspace/instance rows so the next test starts from a virgin DB.</summary>
    public async Task ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Order matters because of FKs.
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM workspace_members; " +
            "DELETE FROM outbox_entries; " +
            "DELETE FROM audit_logs; " +
            "DELETE FROM reminders; " +
            "DELETE FROM contact_tags; " +
            "DELETE FROM contact_circles; " +
            "DELETE FROM contact_methods; " +
            "DELETE FROM notes; " +
            "DELETE FROM interaction_contacts; " +
            "DELETE FROM interactions; " +
            "DELETE FROM attachments; " +
            "DELETE FROM contacts; " +
            "DELETE FROM tags; " +
            "DELETE FROM circles; " +
            "DELETE FROM workspaces; " +
            "DELETE FROM user_sessions; " +
            "DELETE FROM users; " +
            "DELETE FROM instances;");
    }

    /// <summary>Create an HttpClient that authenticates as the given user.</summary>
    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId.ToString());
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
