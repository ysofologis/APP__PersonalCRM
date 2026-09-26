namespace PersonalCrm.Core.Domain;

/// <summary>
/// A named grouping of contacts within a workspace — e.g. "Family", "Mentors", "Work".
/// Scoped to a workspace so the same label can have different meanings across users.
/// </summary>
public class Circle
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Workspace? Workspace { get; set; }
    public ICollection<ContactCircle> Members { get; set; } = [];
}

/// <summary>Join entity: which contacts belong to which circles.</summary>
public class ContactCircle
{
    public Guid ContactId { get; set; }

    public Guid CircleId { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    // Navigation
    public Contact? Contact { get; set; }
    public Circle? Circle { get; set; }
}
