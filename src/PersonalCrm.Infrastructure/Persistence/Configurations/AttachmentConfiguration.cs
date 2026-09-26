using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("attachments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.StorageKey)
               .HasMaxLength(1024)
               .IsRequired();

        builder.Property(a => a.Filename)
               .HasMaxLength(512)
               .IsRequired();

        builder.Property(a => a.ContentType)
               .HasMaxLength(255);

        builder.Property(a => a.Sha256)
               .HasMaxLength(64);             // hex-encoded SHA-256 = 64 chars

        builder.Property(a => a.SizeBytes).IsRequired();

        builder.HasIndex(a => new { a.WorkspaceId, a.CreatedAt })
               .HasDatabaseName("ix_attachments_workspace_created_at");

        builder.HasIndex(a => a.StorageKey)
               .HasDatabaseName("ix_attachments_storage_key");

        builder.HasIndex(a => a.InteractionId)
               .HasDatabaseName("ix_attachments_interaction_id");

        builder.HasIndex(a => a.ContactId)
               .HasDatabaseName("ix_attachments_contact_id");

        builder.HasOne(a => a.Interaction)
               .WithMany(i => i.Attachments)
               .HasForeignKey(a => a.InteractionId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(a => a.Contact)
               .WithMany()
               .HasForeignKey(a => a.ContactId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ActorDisplayName)
               .HasMaxLength(120)
               .IsRequired();

        builder.Property(l => l.Action)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(l => l.EntityType)
               .HasMaxLength(64)
               .IsRequired();

        builder.Property(l => l.EntityId)
               .HasMaxLength(64)
               .IsRequired();

        builder.Property(l => l.Payload)
               .HasColumnType("text");

        builder.Property(l => l.IpAddress)
               .HasMaxLength(64);

        builder.HasIndex(l => new { l.WorkspaceId, l.OccurredAt })
               .HasDatabaseName("ix_audit_logs_workspace_occurred_at");

        builder.HasIndex(l => new { l.EntityType, l.EntityId })
               .HasDatabaseName("ix_audit_logs_entity");

        // Append-only by design — no FK navigation to avoid cycles,
        // no UPDATE/DELETE allowed in code.
    }
}

public class OutboxEntryConfiguration : IEntityTypeConfiguration<OutboxEntry>
{
    public void Configure(EntityTypeBuilder<OutboxEntry> builder)
    {
        builder.ToTable("outbox_entries");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.ClientGeneratedId)
               .IsRequired();

        builder.Property(o => o.OperationType)
               .HasMaxLength(64)
               .IsRequired();

        builder.Property(o => o.PayloadJson)
               .HasColumnType("text")
               .IsRequired();

        builder.Property(o => o.Status)
               .HasConversion<int>()
               .IsRequired();

        builder.HasIndex(o => o.ClientGeneratedId)
               .IsUnique()
               .HasDatabaseName("ux_outbox_client_generated_id");

        builder.HasIndex(o => new { o.Status, o.ReceivedAt })
               .HasDatabaseName("ix_outbox_status_received_at");

        builder.HasIndex(o => new { o.WorkspaceId, o.ActorUserId })
               .HasDatabaseName("ix_outbox_workspace_actor");
    }
}
