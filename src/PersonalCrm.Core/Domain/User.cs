namespace PersonalCrm.Core.Domain;

/// <summary>
/// Represents a registered user on the instance.
/// Each user owns one or more <see cref="Workspace"/>s.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    /// <summary>Argon2id hash of the user's password. Never the plaintext.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? TimeZone { get; set; }

    public string? PreferredLanguage { get; set; }

    public bool IsInstanceAdmin { get; set; }

    public bool IsEmailVerified { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    // Navigation
    public ICollection<UserSession> Sessions { get; set; } = [];
    public ICollection<WorkspaceMember> Memberships { get; set; } = [];
}
