using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence.Configurations;

public class ContactMethodConfiguration : IEntityTypeConfiguration<ContactMethod>
{
    public void Configure(EntityTypeBuilder<ContactMethod> builder)
    {
        builder.ToTable("contact_methods");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Kind)
               .HasConversion<int>()
               .IsRequired();

        builder.Property(m => m.Value)
               .HasMaxLength(2048)
               .IsRequired();

        builder.Property(m => m.Label).HasMaxLength(40);

        builder.Property(m => m.IsPrimary)
               .HasDefaultValue(false);

        builder.HasIndex(m => new { m.ContactId, m.Kind, m.IsPrimary })
               .HasDatabaseName("ix_contact_methods_contact_kind_primary");

        builder.HasIndex(m => m.Value)
               .HasDatabaseName("ix_contact_methods_value");

        builder.HasOne(m => m.Contact)
               .WithMany(c => c.Methods)
               .HasForeignKey(m => m.ContactId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CircleConfiguration : IEntityTypeConfiguration<Circle>
{
    public void Configure(EntityTypeBuilder<Circle> builder)
    {
        builder.ToTable("circles");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
               .HasMaxLength(60)
               .IsRequired();

        builder.Property(c => c.Color).HasMaxLength(16);

        builder.HasIndex(c => new { c.WorkspaceId, c.Name })
               .IsUnique()
               .HasDatabaseName("ux_circles_workspace_name");

        builder.HasOne(c => c.Workspace)
               .WithMany(w => w.Circles)
               .HasForeignKey(c => c.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Members)
               .WithOne(cc => cc.Circle!)
               .HasForeignKey(cc => cc.CircleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContactCircleConfiguration : IEntityTypeConfiguration<ContactCircle>
{
    public void Configure(EntityTypeBuilder<ContactCircle> builder)
    {
        builder.ToTable("contact_circles");

        builder.HasKey(cc => new { cc.ContactId, cc.CircleId });

        builder.HasOne(cc => cc.Contact)
               .WithMany(c => c.Circles)
               .HasForeignKey(cc => cc.ContactId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cc => cc.Circle)
               .WithMany(c => c.Members)
               .HasForeignKey(cc => cc.CircleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
               .HasMaxLength(60)
               .IsRequired();

        builder.Property(t => t.Color).HasMaxLength(16);

        builder.HasIndex(t => new { t.WorkspaceId, t.Name })
               .IsUnique()
               .HasDatabaseName("ux_tags_workspace_name");

        builder.HasOne(t => t.Workspace)
               .WithMany(w => w.Tags)
               .HasForeignKey(t => t.WorkspaceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Contacts)
               .WithOne(ct => ct.Tag!)
               .HasForeignKey(ct => ct.TagId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ContactTagConfiguration : IEntityTypeConfiguration<ContactTag>
{
    public void Configure(EntityTypeBuilder<ContactTag> builder)
    {
        builder.ToTable("contact_tags");

        builder.HasKey(ct => new { ct.ContactId, ct.TagId });

        builder.HasOne(ct => ct.Contact)
               .WithMany(c => c.Tags)
               .HasForeignKey(ct => ct.ContactId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ct => ct.Tag)
               .WithMany(t => t.Contacts)
               .HasForeignKey(ct => ct.TagId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
