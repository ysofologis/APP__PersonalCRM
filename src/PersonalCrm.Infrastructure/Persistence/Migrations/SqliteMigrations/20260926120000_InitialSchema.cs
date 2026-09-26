using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PersonalCrm.Infrastructure.Persistence.Migrations.SqliteMigrations
{
    /// <summary>
    /// Baseline schema for Personal CRM on SQLite.
    /// Mirrors the entity configurations in
    /// <c>PersonalCrm.Infrastructure.Persistence.Configurations</c>.
    ///
    /// Hand-authored here so the project compiles before a contributor runs
    /// <c>dotnet ef migrations add</c> against their own SDK version. When the
    /// codebase reaches a stable v0.1 cut, regenerate via
    /// <c>dotnet ef migrations add InitialSchema --project src/PersonalCrm.Infrastructure --startup-project src/PersonalCrm.Migrations</c>
    /// and replace this file.
    ///
    /// Conventions used in this migration:
    ///   - All PKs are <c>TEXT</c> (UUID v4 strings), produced by the app layer.
    ///   - All timestamps are <c>TEXT</c> in ISO-8601 with offset (SQLite has no native datetime type).
    ///   - All FK columns get an <c>IX_*_id</c> index for query plans.
    ///   - <c>version INTEGER NOT NULL DEFAULT 0</c> columns back EF Core's concurrency token.
    /// </summary>
    public partial class InitialSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // -- Users & sessions ------------------------------------------
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    Id                  = table.Column<string>(nullable: false),
                    Email               = table.Column<string>(maxLength: 320, nullable: false),
                    PasswordHash        = table.Column<string>(maxLength: 512, nullable: false),
                    DisplayName         = table.Column<string>(maxLength: 120, nullable: false, defaultValue: ""),
                    TimeZone            = table.Column<string>(maxLength: 64, nullable: true),
                    PreferredLanguage   = table.Column<string>(maxLength: 16, nullable: true),
                    IsInstanceAdmin     = table.Column<bool>(nullable: false, defaultValue: false),
                    IsEmailVerified     = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt           = table.Column<string>(nullable: false),
                    LastLoginAt         = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_users", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    Id                 = table.Column<string>(nullable: false),
                    UserId             = table.Column<string>(nullable: false),
                    RefreshTokenHash   = table.Column<string>(maxLength: 512, nullable: false),
                    UserAgent          = table.Column<string>(maxLength: 512, nullable: true),
                    IpAddress          = table.Column<string>(maxLength: 64, nullable: true),
                    CreatedAt          = table.Column<string>(nullable: false),
                    ExpiresAt          = table.Column<string>(nullable: false),
                    RevokedAt          = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_sessions", x => x.Id);
                    table.ForeignKey(
                        "FK_user_sessions_users_UserId",
                        x => x.UserId,
                        "users",
                        "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_user_sessions_refresh_token_hash",
                table: "user_sessions",
                column: "RefreshTokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_id",
                table: "user_sessions",
                column: "UserId");

            // -- Workspaces -----------------------------------------------
            migrationBuilder.CreateTable(
                name: "workspaces",
                columns: table => new
                {
                    Id          = table.Column<string>(nullable: false),
                    Name        = table.Column<string>(maxLength: 120, nullable: false),
                    Icon        = table.Column<string>(maxLength: 64, nullable: true),
                    IsPersonal  = table.Column<bool>(nullable: false, defaultValue: false),
                    CreatedAt   = table.Column<string>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_workspaces", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "ix_workspaces_name",
                table: "workspaces",
                column: "Name");

            migrationBuilder.CreateTable(
                name: "workspace_members",
                columns: table => new
                {
                    WorkspaceId = table.Column<string>(nullable: false),
                    UserId      = table.Column<string>(nullable: false),
                    Role        = table.Column<int>(nullable: false),
                    JoinedAt    = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_workspace_members", x => new { x.WorkspaceId, x.UserId });
                    table.ForeignKey("FK_workspace_members_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_workspace_members_users_UserId",           x => x.UserId,      "users",      "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_workspace_members_user_id",
                table: "workspace_members",
                column: "UserId");

            // -- Contacts -------------------------------------------------
            migrationBuilder.CreateTable(
                name: "contacts",
                columns: table => new
                {
                    Id           = table.Column<string>(nullable: false),
                    WorkspaceId  = table.Column<string>(nullable: false),
                    FirstName    = table.Column<string>(maxLength: 120, nullable: false),
                    MiddleName   = table.Column<string>(maxLength: 120, nullable: true),
                    LastName     = table.Column<string>(maxLength: 120, nullable: false),
                    Pronouns     = table.Column<string>(maxLength: 40, nullable: true),
                    PhotoUrl     = table.Column<string>(maxLength: 2048, nullable: true),
                    Birthday     = table.Column<string>(nullable: true),
                    Anniversary  = table.Column<string>(nullable: true),
                    CadenceDays  = table.Column<int>(nullable: true),
                    Version      = table.Column<int>(nullable: false, defaultValue: 0),
                    CreatedAt    = table.Column<string>(nullable: false),
                    UpdatedAt    = table.Column<string>(nullable: false),
                    DeletedAt    = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contacts", x => x.Id);
                    table.ForeignKey("FK_contacts_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_workspace_name",
                table: "contacts",
                columns: new[] { "WorkspaceId", "LastName", "FirstName" });

            migrationBuilder.CreateIndex(
                name: "ix_contacts_deleted_at",
                table: "contacts",
                column: "DeletedAt");

            migrationBuilder.CreateTable(
                name: "contact_methods",
                columns: table => new
                {
                    Id        = table.Column<string>(nullable: false),
                    ContactId = table.Column<string>(nullable: false),
                    Kind      = table.Column<int>(nullable: false),
                    Value     = table.Column<string>(maxLength: 2048, nullable: false),
                    Label     = table.Column<string>(maxLength: 40, nullable: true),
                    IsPrimary = table.Column<bool>(nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_methods", x => x.Id);
                    table.ForeignKey("FK_contact_methods_contacts_ContactId", x => x.ContactId, "contacts", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contact_methods_contact_kind_primary",
                table: "contact_methods",
                columns: new[] { "ContactId", "Kind", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "ix_contact_methods_value",
                table: "contact_methods",
                column: "Value");

            // -- Circles ---------------------------------------------------
            migrationBuilder.CreateTable(
                name: "circles",
                columns: table => new
                {
                    Id           = table.Column<string>(nullable: false),
                    WorkspaceId  = table.Column<string>(nullable: false),
                    Name         = table.Column<string>(maxLength: 60, nullable: false),
                    Color        = table.Column<string>(maxLength: 16, nullable: true),
                    CreatedAt    = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_circles", x => x.Id);
                    table.ForeignKey("FK_circles_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_circles_workspace_name",
                table: "circles",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "contact_circles",
                columns: table => new
                {
                    ContactId = table.Column<string>(nullable: false),
                    CircleId  = table.Column<string>(nullable: false),
                    AddedAt   = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_circles", x => new { x.ContactId, x.CircleId });
                    table.ForeignKey("FK_contact_circles_contacts_ContactId", x => x.ContactId, "contacts", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_contact_circles_circles_CircleId",  x => x.CircleId,  "circles",  "Id", onDelete: ReferentialAction.Cascade);
                });

            // -- Tags ------------------------------------------------------
            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    Id          = table.Column<string>(nullable: false),
                    WorkspaceId = table.Column<string>(nullable: false),
                    Name        = table.Column<string>(maxLength: 60, nullable: false),
                    Color       = table.Column<string>(maxLength: 16, nullable: true),
                    CreatedAt   = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.Id);
                    table.ForeignKey("FK_tags_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_tags_workspace_name",
                table: "tags",
                columns: new[] { "WorkspaceId", "Name" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "contact_tags",
                columns: table => new
                {
                    ContactId = table.Column<string>(nullable: false),
                    TagId     = table.Column<string>(nullable: false),
                    AddedAt   = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_tags", x => new { x.ContactId, x.TagId });
                    table.ForeignKey("FK_contact_tags_contacts_ContactId", x => x.ContactId, "contacts", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_contact_tags_tags_TagId",         x => x.TagId,     "tags",     "Id", onDelete: ReferentialAction.Cascade);
                });

            // -- Notes -----------------------------------------------------
            migrationBuilder.CreateTable(
                name: "notes",
                columns: table => new
                {
                    Id              = table.Column<string>(nullable: false),
                    WorkspaceId     = table.Column<string>(nullable: false),
                    ContactId       = table.Column<string>(nullable: false),
                    ContentMarkdown = table.Column<string>(nullable: false),
                    Version         = table.Column<int>(nullable: false, defaultValue: 0),
                    CreatedAt       = table.Column<string>(nullable: false),
                    UpdatedAt       = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notes", x => x.Id);
                    table.ForeignKey("FK_notes_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_notes_contacts_ContactId",     x => x.ContactId,   "contacts",   "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_notes_contact_updated_at",
                table: "notes",
                columns: new[] { "ContactId", "UpdatedAt" });

            // -- Interactions ---------------------------------------------
            migrationBuilder.CreateTable(
                name: "interactions",
                columns: table => new
                {
                    Id                  = table.Column<string>(nullable: false),
                    WorkspaceId         = table.Column<string>(nullable: false),
                    ClientGeneratedId   = table.Column<string>(nullable: false),
                    Kind                = table.Column<int>(nullable: false),
                    Direction           = table.Column<int>(nullable: false),
                    OccurredAt          = table.Column<string>(nullable: false),
                    DurationMinutes     = table.Column<int>(nullable: true),
                    Location            = table.Column<string>(maxLength: 255, nullable: true),
                    Summary             = table.Column<string>(maxLength: 4096, nullable: true),
                    Version             = table.Column<int>(nullable: false, defaultValue: 0),
                    CreatedAt           = table.Column<string>(nullable: false),
                    UpdatedAt           = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interactions", x => x.Id);
                    table.ForeignKey("FK_interactions_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interactions_workspace_occurred_at",
                table: "interactions",
                columns: new[] { "WorkspaceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "ux_interactions_client_generated_id",
                table: "interactions",
                column: "ClientGeneratedId",
                unique: true);

            migrationBuilder.CreateTable(
                name: "interaction_contacts",
                columns: table => new
                {
                    InteractionId = table.Column<string>(nullable: false),
                    ContactId     = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_interaction_contacts", x => new { x.InteractionId, x.ContactId });
                    table.ForeignKey("FK_interaction_contacts_interactions_InteractionId", x => x.InteractionId, "interactions", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_interaction_contacts_contacts_ContactId",         x => x.ContactId,     "contacts",     "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interaction_contacts_contact_id",
                table: "interaction_contacts",
                column: "ContactId");

            // -- Attachments -----------------------------------------------
            migrationBuilder.CreateTable(
                name: "attachments",
                columns: table => new
                {
                    Id            = table.Column<string>(nullable: false),
                    WorkspaceId   = table.Column<string>(nullable: false),
                    StorageKey    = table.Column<string>(maxLength: 1024, nullable: false),
                    Filename      = table.Column<string>(maxLength: 512, nullable: false),
                    ContentType   = table.Column<string>(maxLength: 255, nullable: true),
                    SizeBytes     = table.Column<long>(nullable: false),
                    Sha256        = table.Column<string>(maxLength: 64, nullable: true),
                    InteractionId = table.Column<string>(nullable: true),
                    ContactId     = table.Column<string>(nullable: true),
                    CreatedAt     = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attachments", x => x.Id);
                    table.ForeignKey("FK_attachments_interactions_InteractionId", x => x.InteractionId, "interactions", "Id", onDelete: ReferentialAction.SetNull);
                    table.ForeignKey("FK_attachments_contacts_ContactId",         x => x.ContactId,     "contacts",     "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attachments_workspace_created_at",
                table: "attachments",
                columns: new[] { "WorkspaceId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_attachments_storage_key",
                table: "attachments",
                column: "StorageKey");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_interaction_id",
                table: "attachments",
                column: "InteractionId");

            migrationBuilder.CreateIndex(
                name: "ix_attachments_contact_id",
                table: "attachments",
                column: "ContactId");

            // -- Reminders ------------------------------------------------
            migrationBuilder.CreateTable(
                name: "reminders",
                columns: table => new
                {
                    Id            = table.Column<string>(nullable: false),
                    WorkspaceId   = table.Column<string>(nullable: false),
                    ContactId     = table.Column<string>(nullable: true),
                    Kind          = table.Column<int>(nullable: false),
                    Title         = table.Column<string>(maxLength: 255, nullable: false),
                    Notes         = table.Column<string>(maxLength: 4096, nullable: true),
                    DueAt         = table.Column<string>(nullable: false),
                    SnoozedUntil  = table.Column<string>(nullable: true),
                    CompletedAt   = table.Column<string>(nullable: true),
                    CreatedAt     = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reminders", x => x.Id);
                    table.ForeignKey("FK_reminders_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Restrict);
                    table.ForeignKey("FK_reminders_contacts_ContactId",     x => x.ContactId,   "contacts",   "Id", onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reminders_workspace_due_at",
                table: "reminders",
                columns: new[] { "WorkspaceId", "DueAt" });

            migrationBuilder.CreateIndex(
                name: "ix_reminders_contact_due_at",
                table: "reminders",
                columns: new[] { "ContactId", "DueAt" });

            // -- Audit log ------------------------------------------------
            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    Id                = table.Column<string>(nullable: false),
                    WorkspaceId       = table.Column<string>(nullable: false),
                    ActorUserId       = table.Column<string>(nullable: true),
                    ActorDisplayName  = table.Column<string>(maxLength: 120, nullable: false),
                    Action            = table.Column<int>(nullable: false),
                    EntityType        = table.Column<string>(maxLength: 64, nullable: false),
                    EntityId          = table.Column<string>(maxLength: 64, nullable: false),
                    Payload           = table.Column<string>(nullable: true),
                    IpAddress         = table.Column<string>(maxLength: 64, nullable: true),
                    OccurredAt        = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                    table.ForeignKey("FK_audit_logs_workspaces_WorkspaceId", x => x.WorkspaceId, "workspaces", "Id", onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_workspace_occurred_at",
                table: "audit_logs",
                columns: new[] { "WorkspaceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_entity",
                table: "audit_logs",
                columns: new[] { "EntityType", "EntityId" });

            // -- Outbox ---------------------------------------------------
            migrationBuilder.CreateTable(
                name: "outbox_entries",
                columns: table => new
                {
                    Id                  = table.Column<string>(nullable: false),
                    WorkspaceId         = table.Column<string>(nullable: false),
                    ActorUserId         = table.Column<string>(nullable: false),
                    ClientGeneratedId   = table.Column<string>(nullable: false),
                    OperationType       = table.Column<string>(maxLength: 64, nullable: false),
                    PayloadJson         = table.Column<string>(nullable: false),
                    Status              = table.Column<int>(nullable: false),
                    ReceivedAt          = table.Column<string>(nullable: false),
                    AppliedAt           = table.Column<string>(nullable: true),
                    Error               = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_outbox_entries", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "ux_outbox_client_generated_id",
                table: "outbox_entries",
                column: "ClientGeneratedId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_outbox_status_received_at",
                table: "outbox_entries",
                columns: new[] { "Status", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_workspace_actor",
                table: "outbox_entries",
                columns: new[] { "WorkspaceId", "ActorUserId" });

            // -- Instance (singleton) ---------------------------------------
            migrationBuilder.CreateTable(
                name: "instances",
                columns: table => new
                {
                    Id                = table.Column<string>(nullable: false),
                    SiteName          = table.Column<string>(maxLength: 120, nullable: false),
                    DefaultLanguage   = table.Column<string>(maxLength: 16, nullable: true),
                    CreatedAt         = table.Column<string>(nullable: false),
                    SetupCompletedAt  = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_instances", x => x.Id));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop in reverse FK order so SQLite doesn't complain about
            // dependent tables existing.
            migrationBuilder.DropTable("instances");
            migrationBuilder.DropTable("outbox_entries");
            migrationBuilder.DropTable("audit_logs");
            migrationBuilder.DropTable("reminders");
            migrationBuilder.DropTable("attachments");
            migrationBuilder.DropTable("interaction_contacts");
            migrationBuilder.DropTable("interactions");
            migrationBuilder.DropTable("notes");
            migrationBuilder.DropTable("contact_tags");
            migrationBuilder.DropTable("tags");
            migrationBuilder.DropTable("contact_circles");
            migrationBuilder.DropTable("circles");
            migrationBuilder.DropTable("contact_methods");
            migrationBuilder.DropTable("contacts");
            migrationBuilder.DropTable("workspace_members");
            migrationBuilder.DropTable("workspaces");
            migrationBuilder.DropTable("user_sessions");
            migrationBuilder.DropTable("users");
        }
    }
}
