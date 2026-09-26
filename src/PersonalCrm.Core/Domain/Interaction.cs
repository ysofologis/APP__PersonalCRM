namespace PersonalCrm.Core.Domain;

/// <summary>
/// A logged event with one or more contacts — a call, meeting, message, gift, etc.
/// Forms the reverse-chronological timeline per contact and the global activity feed.
/// </summary>
public class Interaction
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    /// <summary>Client-generated UUIDv7. Used as the idempotency key for offline sync.</summary>
    public Guid ClientGeneratedId { get; set; }

    public InteractionKind Kind { get; set; }

    public InteractionDirection Direction { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public int? DurationMinutes { get; set; }

    public string? Location { get; set; }

    public string? Summary { get; set; }

    public int Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public Workspace? Workspace { get; set; }
    public ICollection<InteractionContact> Participants { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
}

/// <summary>Join entity: which contacts participated in an interaction.</summary>
public class InteractionContact
{
    public Guid InteractionId { get; set; }

    public Guid ContactId { get; set; }

    // Navigation
    public Interaction? Interaction { get; set; }
    public Contact? Contact { get; set; }
}

public enum InteractionKind
{
    Call    = 0,
    Meeting = 1,
    Email   = 2,
    Message = 3,
    Gift    = 4,
    Event   = 5,
    Other   = 99
}

public enum InteractionDirection
{
    Outbound = 0,
    Inbound  = 1,
    Neutral  = 2
}
