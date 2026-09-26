namespace PersonalCrm.Core.Domain;

/// <summary>
/// Per-instance configuration row. There is at most one of these per database.
/// Stores settings that don't belong on a workspace or a user — e.g. the site
/// name, the default language, the activation timestamp, and a flag indicating
/// whether the first-run wizard has completed.
/// </summary>
public class Instance
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string SiteName { get; set; } = "Personal CRM";

    public string? DefaultLanguage { get; set; } = "en-US";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SetupCompletedAt { get; set; }
}
