using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Infrastructure.Persistence.WorkspaceContext;

namespace PersonalCrm.App.Auth;

/// <summary>
/// Default <see cref="IWorkspaceContext"/> for HTTP requests. Resolves the
/// caller from <see cref="ClaimsPrincipal"/>, loads their workspace
/// memberships from the database, and exposes them to the rest of the app
/// (notably EF Core query filters).
///
/// <para>
/// Registered as <c>scoped</c>. One instance per HTTP request, populated
/// once on first access and cached for the request lifetime.
/// </para>
/// </summary>
public sealed class HttpWorkspaceContext : IWorkspaceContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HttpWorkspaceContext> _logger;

    private readonly Lazy<ContextData> _data;

    public HttpWorkspaceContext(
        IHttpContextAccessor httpContextAccessor,
        IServiceScopeFactory scopeFactory,
        ILogger<HttpWorkspaceContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _scopeFactory        = scopeFactory;
        _logger              = logger;
        _data                = new Lazy<ContextData>(LoadFromDatabase, isThreadSafe: true);
    }

    // ----- IWorkspaceContext ------------------------------------------------

    public Guid UserId => _data.Value.UserId;

    public string? DisplayName => _data.Value.DisplayName;

    public bool IsInstanceAdmin => _data.Value.IsInstanceAdmin;

    public IReadOnlySet<Guid> AccessibleWorkspaceIds => _data.Value.AccessibleWorkspaceIds;

    public Guid? CurrentWorkspaceId => _data.Value.CurrentWorkspaceId;

    public bool CanRead(Guid workspaceId)
        => IsInstanceAdmin || AccessibleWorkspaceIds.Contains(workspaceId);

    public bool CanWrite(Guid workspaceId, WorkspaceRole required = WorkspaceRole.Editor)
    {
        if (IsInstanceAdmin) return true;
        if (!_data.Value.Memberships.TryGetValue(workspaceId, out var role)) return false;
        return role >= required;
    }

    // ----- helpers ----------------------------------------------------------

    private sealed class ContextData
    {
        public Guid UserId { get; init; }
        public string? DisplayName { get; init; }
        public bool IsInstanceAdmin { get; init; }
        public IReadOnlySet<Guid> AccessibleWorkspaceIds { get; init; } = new HashSet<Guid>();
        public Guid? CurrentWorkspaceId { get; init; }
        public IReadOnlyDictionary<Guid, WorkspaceRole> Memberships { get; init; }
            = new Dictionary<Guid, WorkspaceRole>();
    }

    private ContextData LoadFromDatabase()
    {
        var http = _httpContextAccessor.HttpContext;
        var user = http?.User;

        // Unauthenticated request — return an empty context. Endpoints will
        // respond 401 if they require auth.
        if (user?.Identity?.IsAuthenticated != true)
        {
            return new ContextData();
        }

        var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? user.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            _logger.LogWarning("Authenticated principal has no usable user-id claim.");
            return new ContextData();
        }

        // Resolve memberships via a short-lived DbContext scope so we don't
        // share a context with the request handler (different lifetime).
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var userRow = db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.DisplayName, u.IsInstanceAdmin })
            .FirstOrDefault();

        if (userRow is null)
        {
            _logger.LogWarning("User {UserId} in token but missing in DB.", userId);
            return new ContextData();
        }

        var memberships = db.WorkspaceMembers
            .AsNoTracking()
            .Where(m => m.UserId == userId)
            .Select(m => new { m.WorkspaceId, m.Role })
            .ToList();

        var dict  = memberships.ToDictionary(m => m.WorkspaceId, m => m.Role);
        var ids   = new HashSet<Guid>(dict.Keys);

        // The "current" workspace can be hinted by the route or a header.
        // For MVP we read it from a header (X-Workspace-Id). Production will
        // also accept it from the URL segment and a signed cookie.
        Guid? current = null;
        if (http.Request.Headers.TryGetValue("X-Workspace-Id", out var wsHeader)
            && Guid.TryParse(wsHeader.ToString(), out var wsId)
            && ids.Contains(wsId))
        {
            current = wsId;
        }

        return new ContextData
        {
            UserId                 = userRow.Id,
            DisplayName            = userRow.DisplayName,
            IsInstanceAdmin        = userRow.IsInstanceAdmin,
            AccessibleWorkspaceIds = ids,
            CurrentWorkspaceId     = current,
            Memberships            = dict
        };
    }
}
