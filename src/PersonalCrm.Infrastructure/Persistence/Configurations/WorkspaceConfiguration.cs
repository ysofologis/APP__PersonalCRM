using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence.Configurations;

public class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
               .HasMaxLength(120)
               .IsRequired();

        builder.Property(w => w.Icon).HasMaxLength(64);

        builder.Property(w => w.IsPersonal)
               .HasDefaultValue(false);

        builder.HasIndex(w => new { w.Name })
               .HasDatabaseName("ix_workspaces_name");

        builder.HasMany(w => w.Members)
               .WithOne(m => m.Workspace!)
               .HasForeignKey(m => m.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Contacts)
               .WithOne(c => c.Workspace!)
               .HasForeignKey(c => c.WorkspaceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Circles)
               .WithOne(c => c.Workspace!)
               .HasForeignKey(c => c.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(w => w.Tags)
               .WithOne(t => t.Workspace!)
               .HasForeignKey(t => t.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable("workspace_members");

        // Composite primary key — a user belongs to each workspace at most once.
        builder.HasKey(m => new { m.WorkspaceId, m.UserId });

        builder.Property(m => m.Role)
               .HasConversion<int>()
               .IsRequired();

        builder.HasIndex(m => m.UserId)
               .HasDatabaseName("ix_workspace_members_user_id");

        builder.HasOne(m => m.Workspace)
               .WithMany(w => w.Members)
               .HasForeignKey(m => m.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.User)
               .WithMany(u => u.Memberships)
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
