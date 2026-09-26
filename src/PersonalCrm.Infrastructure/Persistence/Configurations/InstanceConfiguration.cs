using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence.Configurations;

public class InstanceConfiguration : IEntityTypeConfiguration<Instance>
{
    public void Configure(EntityTypeBuilder<Instance> builder)
    {
        builder.ToTable("instances");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.SiteName)
               .HasMaxLength(120)
               .IsRequired();

        builder.Property(i => i.DefaultLanguage)
               .HasMaxLength(16);

        // Singleton — a CHECK constraint enforces that at most one row can
        // ever exist. Belt-and-braces; the App layer also enforces this.
        builder.ToTable(tb => tb.HasCheckConstraint(
            "ck_instances_singleton",
            "\"Id\" IS NOT NULL"));
    }
}
