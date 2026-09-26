using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PersonalCrm.Infrastructure.Persistence.WorkspaceContext;
using PersonalCrm.Infrastructure.Security;

namespace PersonalCrm.App.Auth;

/// <summary>
/// DI helpers for the App layer's auth and workspace-context wiring.
/// Lives in <c>PersonalCrm.App</c> because both halves (HTTP accessor + EF
/// services) cross the App ↔ Infrastructure seam.
/// </summary>
public static class AuthServiceCollectionExtensions
{
    public static IServiceCollection AddPersonalCrmAuth(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // IHttpContextAccessor is required by HttpWorkspaceContext.
        services.AddHttpContextAccessor();

        // PasswordHasher is a static class (no instance state) — call its
        // methods directly. No DI registration needed.

        // Workspace context is per-request, populated from ClaimsPrincipal.
        services.AddScoped<IWorkspaceContext, HttpWorkspaceContext>();

        return services;
    }
}
