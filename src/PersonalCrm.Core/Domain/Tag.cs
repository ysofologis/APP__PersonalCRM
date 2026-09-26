namespace PersonalCrm.Core.Domain;

/// <summary>
/// A free-form, workspace-scoped label attached to contacts.
/// Unlike <see cref="Circle"/>, tags are flat and not groupable.
/// </summary>
public class Tag
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Color { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Workspace? Workspace { get; set; }
    public ICollection<ContactTag> Contacts { get; set; } = [];
}

/// <summary>Join entity: which contacts carry which tags.</summary>
public class ContactTag
{
    public Guid ContactId { get; set; }

    public Guid TagId { get; set; }

    public DateTimeOffset AddedAt { get; set; }

    // Navigation
    public Contact? Contact { get; set; }
    public Tag? Tag { get; set; }
}
