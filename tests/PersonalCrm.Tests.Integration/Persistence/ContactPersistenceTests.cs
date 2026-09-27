using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Tests.Integration.Infrastructure;
using Xunit;

namespace PersonalCrm.Tests.Integration.Persistence;

/// <summary>
/// Round-trip a contact through EF Core against an in-memory SQLite DB.
/// Exercises the new <c>ContactConfiguration</c> + InitialSchema migration.
/// </summary>
public class ContactPersistenceTests : IClassFixture<CrmWebAppFactory>
{
    private readonly CrmWebAppFactory _factory;

    public ContactPersistenceTests(CrmWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Round_trip_preserves_all_fields()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Make sure schema is there (shared fixture already migrated; this
        // makes the test robust if run in isolation).
        await db.Database.MigrateAsync();

        var workspaceId = Guid.NewGuid();
        var now         = DateTimeOffset.UtcNow;

        db.Workspaces.Add(new Workspace
        {
            Id         = workspaceId,
            Name       = $"ws-{Guid.NewGuid():N}",
            CreatedAt  = now
        });

        var contactId = Guid.NewGuid();
        var contact   = new Contact
        {
            Id          = contactId,
            WorkspaceId = workspaceId,
            FirstName   = "Ada",
            MiddleName  = "Augusta",
            LastName    = "Lovelace",
            Pronouns    = "she/her",
            Birthday    = new DateOnly(1815, 12, 10),
            Anniversary = null,
            CadenceDays = 30,
            Version     = 0,
            CreatedAt   = now,
            UpdatedAt   = now,
            Methods =
            {
                new ContactMethod
                {
                    Id        = Guid.NewGuid(),
                    Kind      = ContactMethodKind.Email,
                    Value     = "ada@example.com",
                    Label     = "work",
                    IsPrimary = true
                }
            }
        };

        db.Contacts.Add(contact);
        await db.SaveChangesAsync();

        // Detach to ensure we read from the database, not the change tracker.
        foreach (var entry in db.ChangeTracker.Entries().ToList())
        {
            entry.State = EntityState.Detached;
        }

        var loaded = await db.Contacts
            .Include(c => c.Methods)
            .FirstAsync(c => c.Id == contactId);

        loaded.FirstName.Should().Be("Ada");
        loaded.MiddleName.Should().Be("Augusta");
        loaded.LastName.Should().Be("Lovelace");
        loaded.FullName.Should().Be("Ada Augusta Lovelace");
        loaded.Birthday.Should().Be(new DateOnly(1815, 12, 10));
        loaded.CadenceDays.Should().Be(30);
        loaded.Methods.Should().HaveCount(1);
        loaded.Methods.Single().Value.Should().Be("ada@example.com");
        loaded.Methods.Single().IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Unique_email_constraint_is_enforced()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Database.MigrateAsync();

        var email = $"dup-{Guid.NewGuid():N}@example.com";

        db.Users.Add(new User
        {
            Id           = Guid.NewGuid(),
            Email        = email,
            PasswordHash = "$argon2id$v=19$m=65536,t=3,p=4$AAAAAAAAAAAAAAAAAAAAAA$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            DisplayName  = "First",
            CreatedAt    = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        db.Users.Add(new User
        {
            Id           = Guid.NewGuid(),
            Email        = email,
            PasswordHash = "$argon2id$v=19$m=65536,t=3,p=4$BBBBBBBBBBBBBBBBBBBBBB$BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
            DisplayName  = "Second",
            CreatedAt    = DateTimeOffset.UtcNow
        });

        var act = async () => await db.SaveChangesAsync();
        await act.Should().ThrowAsync<DbUpdateException>();
    }
}
