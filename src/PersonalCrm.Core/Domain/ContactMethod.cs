namespace PersonalCrm.Core.Domain;

/// <summary>
/// A way to reach a contact — email, phone, address, social handle, website.
/// Multi-valued; one can be flagged as primary.
/// </summary>
public class ContactMethod
{
    public Guid Id { get; set; }

    public Guid ContactId { get; set; }

    public ContactMethodKind Kind { get; set; }

    public string Value { get; set; } = string.Empty;

    /// <summary>Optional structured label, e.g. "work", "personal".</summary>
    public string? Label { get; set; }

    public bool IsPrimary { get; set; }

    // Navigation
    public Contact? Contact { get; set; }
}

public enum ContactMethodKind
{
    Email       = 0,
    Phone       = 1,
    Address     = 2,
    Social      = 3,
    Website     = 4,
    Other       = 5
}
