namespace PersonalCrm.Core.Domain;

/// <summary>
/// A user's membership in a workspace, with a role. Enforces workspace-level
/// authorization (Owner &gt; Admin &gt; Editor &gt; Viewer).
/// </summary>
public class WorkspaceMember
{
    public Guid Id { get; set; }

    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public WorkspaceRole Role { get; set; }

    public DateTimeOffset JoinedAt { get; set; }

    // Navigation
    public Workspace? Workspace { get; set; }
    public User? User { get; set; }
}

public enum WorkspaceRole
{
    Viewer = 0,
    Editor = 1,
    Admin  = 2,
    Owner  = 3
}
