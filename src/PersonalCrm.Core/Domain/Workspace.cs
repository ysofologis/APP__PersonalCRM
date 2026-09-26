namespace PersonalCrm.Core.Domain;

/// <summary>
/// A logical container of contacts owned by one or more users.
/// Every user has a default private workspace on signup and may create more.
/// </summary>
public class Workspace
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Icon { get; set; }

    /// <summary>True for the workspace automatically created for a user on signup.</summary>
    public bool IsPersonal { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public ICollection<WorkspaceMember> Members { get; set; } = [];
    public ICollection<Contact> Contacts { get; set; } = [];
    public ICollection<Circle> Circles { get; set; } = [];
    public ICollection<Tag> Tags { get; set; } = [];
}
