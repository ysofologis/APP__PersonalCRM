using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalCrm.Core.Domain;

namespace PersonalCrm.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
               .HasMaxLength(320)        // RFC 5321 max length
               .IsRequired();

        builder.HasIndex(u => u.Email)
               .IsUnique()
               .HasDatabaseName("ux_users_email");

        builder.Property(u => u.PasswordHash)
               .HasMaxLength(512)
               .IsRequired();

        builder.Property(u => u.DisplayName)
               .HasMaxLength(120)
               .IsRequired()
               .HasDefaultValue(string.Empty);

        builder.Property(u => u.TimeZone)
               .HasMaxLength(64);

        builder.Property(u => u.PreferredLanguage)
               .HasMaxLength(16);

        builder.Property(u => u.IsInstanceAdmin)
               .HasDefaultValue(false);

        builder.Property(u => u.IsEmailVerified)
               .HasDefaultValue(false);

        // Sessions
        builder.HasMany(u => u.Sessions)
               .WithOne(s => s.User!)
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Cascade);

        // Workspace memberships
        builder.HasMany(u => u.Memberships)
               .WithOne(m => m.User!)
               .HasForeignKey(m => m.UserId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserSessionConfiguration : IEntityTypeConfiguration<UserSession>
{
    public void Configure(EntityTypeBuilder<UserSession> builder)
    {
        builder.ToTable("user_sessions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.RefreshTokenHash)
               .HasMaxLength(512)
               .IsRequired();

        builder.HasIndex(s => s.RefreshTokenHash)
               .IsUnique()
               .HasDatabaseName("ux_user_sessions_refresh_token_hash");

        builder.HasIndex(s => s.UserId)
               .HasDatabaseName("ix_user_sessions_user_id");

        builder.Property(s => s.UserAgent).HasMaxLength(512);
        builder.Property(s => s.IpAddress).HasMaxLength(64);     // IPv6 max
    }
}
