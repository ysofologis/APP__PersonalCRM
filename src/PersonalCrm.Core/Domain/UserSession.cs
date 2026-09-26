namespace PersonalCrm.Core.Domain;

/// <summary>
/// Represents an active authenticated session for a <see cref="User"/>.
/// Refresh tokens are stored hashed; the plaintext only exists in the
/// httpOnly cookie on the client.
/// </summary>
public class UserSession
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    /// <summary>Argon2id hash of the refresh token. Never the plaintext.</summary>
    public string RefreshTokenHash { get; set; } = string.Empty;

    public string? UserAgent { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    // Navigation
    public User? User { get; set; }
}
