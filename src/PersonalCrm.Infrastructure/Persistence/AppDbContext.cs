using Microsoft.EntityFrameworkCore;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence;

/// <summary>
/// Application DbContext. SQLite by default; the same model works on Postgres
/// (see ADR 0002) — only the provider registration changes.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ---- Users & sessions ----
    public DbSet<User>           Users            => Set<User>();
    public DbSet<UserSession>    UserSessions     => Set<UserSession>();

    // ---- Workspaces ----
    public DbSet<Workspace>        Workspaces     => Set<Workspace>();
    public DbSet<WorkspaceMember>  WorkspaceMembers => Set<WorkspaceMember>();

    // ---- Contacts ----
    public DbSet<Contact>              Contacts          => Set<Contact>();
    public DbSet<ContactMethod>        ContactMethods    => Set<ContactMethod>();
    public DbSet<Circle>               Circles           => Set<Circle>();
    public DbSet<ContactCircle>        ContactCircles    => Set<ContactCircle>();
    public DbSet<Tag>                  Tags              => Set<Tag>();
    public DbSet<ContactTag>           ContactTags       => Set<ContactTag>();
    public DbSet<Note>                 Notes             => Set<Note>();

    // ---- Interactions ----
    public DbSet<Interaction>        Interactions     => Set<Interaction>();
    public DbSet<InteractionContact> InteractionContacts => Set<InteractionContact>();
    public DbSet<Attachment>         Attachments      => Set<Attachment>();

    // ---- Reminders ----
    public DbSet<Reminder> Reminders => Set<Reminder>();

    // ---- Cross-cutting ----
    public DbSet<AuditLog>     AuditLogs    => Set<AuditLog>();
    public DbSet<OutboxEntry>  Outbox       => Set<OutboxEntry>();
    public DbSet<Instance>     Instances    => Set<Instance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Workspace-isolation query filter is applied per-entity in
        // configurations so each navigation can declare its own predicate.
        // The set of accessible workspace ids is provided per-request by a
        // scoped IWorkspaceContext implementation registered in the App layer.

        base.OnModelCreating(modelBuilder);
    }
}
