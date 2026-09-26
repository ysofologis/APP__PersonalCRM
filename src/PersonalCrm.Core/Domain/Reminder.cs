namespace PersonalCrm.Core.Domain;

/// <summary>
/// A reminder surfaced to a user at a specific time — birthday, anniversary,
/// stay-in-touch cadence lapse, or a one-off note-to-self.
/// </summary>
public class Reminder
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    /// <summary>Optional. If set, this reminder fires when the cadence with this contact lapses.</summary>
    public Guid? ContactId { get; set; }

    public ReminderKind Kind { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Notes { get; set; }

    public DateTimeOffset DueAt { get; set; }

    public DateTimeOffset? SnoozedUntil { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Workspace? Workspace { get; set; }
    public Contact? Contact { get; set; }
}

public enum ReminderKind
{
    OneOff         = 0,
    CadenceLapse   = 1,
    Birthday       = 2,
    Anniversary    = 3,
    CustomDate     = 4
}
