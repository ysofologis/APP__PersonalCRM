namespace PersonalCrm.Core.Domain;

/// <summary>
/// Append-only record of every mutation in a workspace. Used for:
///   - the per-workspace activity feed ("Alice updated Bob's phone number"),
///   - the instance-level audit log (admins / security review),
///   - compliance ("who deleted this contact?").
/// </summary>
public class AuditLog
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid? ActorUserId { get; set; }

    public string ActorDisplayName { get; set; } = string.Empty;

    public AuditAction Action { get; set; }

    /// <summary>Name of the entity affected, e.g. <c>"Contact"</c>.</summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Stringified id of the affected entity, e.g. <c>"3fa85f64-..."</c>.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Free-form change summary, JSON if structured.</summary>
    public string? Payload { get; set; }

    public string? IpAddress { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    // Navigation
    public Workspace? Workspace { get; set; }
}

public enum AuditAction
{
    Create      = 0,
    Update      = 1,
    Delete      = 2,
    Restore     = 3,
    Merge       = 4,
    Login       = 10,
    Logout      = 11,
    Invite      = 20,
    Join        = 21,
    Leave       = 22,
    RoleChange  = 23,
    Export      = 30,
    Import      = 31
}
