using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence.Configurations;

public class InteractionConfiguration : IEntityTypeConfiguration<Interaction>
{
    public void Configure(EntityTypeBuilder<Interaction> builder)
    {
        builder.ToTable("interactions");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.ClientGeneratedId)
               .IsRequired();

        builder.Property(i => i.Kind)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(i => i.Direction)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(i => i.Location).HasMaxLength(255);
        builder.Property(i => i.Summary).HasMaxLength(4096);

        builder.Property(i => i.Version)
               .IsConcurrencyToken();

        builder.HasIndex(i => new { i.WorkspaceId, i.OccurredAt })
               .HasDatabaseName("ix_interactions_workspace_occurred_at");

        builder.HasIndex(i => i.ClientGeneratedId)
               .IsUnique()
               .HasDatabaseName("ux_interactions_client_generated_id");

        builder.HasOne(i => i.Workspace)
               .WithMany()
               .HasForeignKey(i => i.WorkspaceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(i => i.Participants)
               .WithOne(ic => ic.Interaction!)
               .HasForeignKey(ic => ic.InteractionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(i => i.Attachments)
               .WithOne(a => a.Interaction!)
               .HasForeignKey(a => a.InteractionId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class InteractionContactConfiguration : IEntityTypeConfiguration<InteractionContact>
{
    public void Configure(EntityTypeBuilder<InteractionContact> builder)
    {
        builder.ToTable("interaction_contacts");

        builder.HasKey(ic => new { ic.InteractionId, ic.ContactId });

        builder.HasOne(ic => ic.Interaction)
               .WithMany(i => i.Participants)
               .HasForeignKey(ic => ic.InteractionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ic => ic.Contact)
               .WithMany(c => c.Interactions)
               .HasForeignKey(ic => ic.ContactId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ic => ic.ContactId)
               .HasDatabaseName("ix_interaction_contacts_contact_id");
    }
}

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> builder)
    {
        builder.ToTable("notes");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.ContentMarkdown)
               .HasColumnType("text")    // SQLite TEXT; PostgreSQL TEXT equivalent
               .IsRequired();

        builder.Property(n => n.Version)
               .IsConcurrencyToken();

        builder.HasIndex(n => new { n.ContactId, n.UpdatedAt })
               .HasDatabaseName("ix_notes_contact_updated_at");

        builder.HasOne(n => n.Workspace)
               .WithMany()
               .HasForeignKey(n => n.WorkspaceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(n => n.Contact)
               .WithMany(c => c.Notes)
               .HasForeignKey(n => n.ContactId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.ToTable("reminders");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Kind)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(r => r.Title)
               .HasMaxLength(255)
               .IsRequired();

        builder.Property(r => r.Notes)
               .HasMaxLength(4096);

        builder.HasIndex(r => new { r.WorkspaceId, r.DueAt })
               .HasDatabaseName("ix_reminders_workspace_due_at");

        builder.HasIndex(r => new { r.ContactId, r.DueAt })
               .HasDatabaseName("ix_reminders_contact_due_at");

        builder.HasOne(r => r.Workspace)
               .WithMany()
               .HasForeignKey(r => r.WorkspaceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Contact)
               .WithMany(c => c.Reminders)
               .HasForeignKey(r => r.ContactId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
