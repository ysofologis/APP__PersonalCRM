namespace PersonalCrm.Core.Domain;

/// <summary>
/// A person tracked in a workspace. Soft-deletable; recoverable within 30 days.
/// Relationship strength is **not** stored here — it's computed on demand
/// (see <see cref="RelationshipStrength"/>).
/// </summary>
public class Contact
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    public string LastName { get; set; } = string.Empty;

    public string? Pronouns { get; set; }

    public string? PhotoUrl { get; set; }

    public DateOnly? Birthday { get; set; }

    public DateOnly? Anniversary { get; set; }

    /// <summary>Stay-in-touch cadence in days. Null means "no cadence".</summary>
    public int? CadenceDays { get; set; }

    /// <summary>Monotonic version, bumped on every mutation. Used for offline-sync conflict detection.</summary>
    public int Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public string FullName =>
        string.Join(' ', new[] { FirstName, MiddleName, LastName }
            .Where(s => !string.IsNullOrWhiteSpace(s)));

    // Navigation
    public Workspace? Workspace { get; set; }
    public ICollection<ContactMethod> Methods { get; set; } = [];
    public ICollection<InteractionContact> Interactions { get; set; } = [];
    public ICollection<ContactCircle> Circles { get; set; } = [];
    public ICollection<ContactTag> Tags { get; set; } = [];
    public ICollection<Note> Notes { get; set; } = [];
    public ICollection<Reminder> Reminders { get; set; } = [];
}
