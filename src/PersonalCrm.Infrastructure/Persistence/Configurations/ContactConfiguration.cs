using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for <see cref="Contact"/>. Demonstrates the
/// conventions used throughout the persistence layer:
///   - string columns explicitly sized to avoid NVARCHAR(MAX) drift,
///   - indexes declared for the access patterns we actually use,
///   - cascade deletes scoped to the workspace boundary.
/// </summary>
public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.WorkspaceId).IsRequired();

        builder.Property(c => c.FirstName).HasMaxLength(120).IsRequired();
        builder.Property(c => c.MiddleName).HasMaxLength(120);
        builder.Property(c => c.LastName).HasMaxLength(120).IsRequired();
        builder.Property(c => c.Pronouns).HasMaxLength(40);
        builder.Property(c => c.PhotoUrl).HasMaxLength(2048);

        builder.Property(c => c.Version).IsConcurrencyToken();

        builder.HasIndex(c => new { c.WorkspaceId, c.LastName, c.FirstName })
               .HasDatabaseName("ix_contacts_workspace_name");

        builder.HasIndex(c => c.DeletedAt)
               .HasDatabaseName("ix_contacts_deleted_at");

        // Relationships
        builder.HasOne(c => c.Workspace)
               .WithMany(w => w.Contacts)
               .HasForeignKey(c => c.WorkspaceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Methods)
               .WithOne(m => m.Contact!)
               .HasForeignKey(m => m.ContactId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Interactions)
               .WithOne(ic => ic.Contact!)
               .HasForeignKey(ic => ic.ContactId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Notes)
               .WithOne(n => n.Contact!)
               .HasForeignKey(n => n.ContactId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Reminders)
               .WithOne(r => r.Contact!)
               .HasForeignKey(r => r.ContactId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}
