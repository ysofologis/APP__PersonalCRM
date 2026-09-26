using System.Security.Claims;

namespace PersonalCrm.Infrastructure.Persistence.WorkspaceContext;

/// <summary>
/// Per-request view of the calling user and the workspaces they can see.
/// The App layer registers a scoped implementation that reads
/// <see cref="ClaimsPrincipal"/> and resolves memberships from the database.
/// EF Core query filters (added in a follow-up commit) use this to scope
/// every read/write to the caller's accessible workspaces.
/// </summary>
public interface IWorkspaceContext
{
    Guid UserId { get; }

    string? DisplayName { get; }

    bool IsInstanceAdmin { get; }

    /// <summary>Set of workspace ids the user can see. Empty for anonymous.</summary>
    IReadOnlySet<Guid> AccessibleWorkspaceIds { get; }

    /// <summary>Active workspace for this request (used as default in single-workspace routes).</summary>
    Guid? CurrentWorkspaceId { get; }

    /// <summary>True if the user can read the given workspace.</summary>
    bool CanRead(Guid workspaceId);

    /// <summary>True if the user can mutate the given workspace at the requested role.</summary>
    bool CanWrite(Guid workspaceId, WorkspaceRole required = WorkspaceRole.Editor);
}
