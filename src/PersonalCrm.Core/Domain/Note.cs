namespace PersonalCrm.Core.Domain;

/// <summary>
/// A rich-text (Markdown) note attached to a contact. Stored as plain Markdown
/// in TEXT; the WASM client renders to HTML with a sanitiser.
/// </summary>
public class Note
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid ContactId { get; set; }

    public string ContentMarkdown { get; set; } = string.Empty;

    public int Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    // Navigation
    public Workspace? Workspace { get; set; }
    public Contact? Contact { get; set; }
}
