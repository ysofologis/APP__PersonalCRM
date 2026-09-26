using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Infrastructure.Security;

namespace PersonalCrm.App.Endpoints;

/// <summary>
/// First-run setup + auth endpoints for v0.1. Wires together the password
/// hasher, JWT signing (placeholder for v0.2 refresh-token rotation), and
/// the per-instance <c>Instance</c> row that flips on wizard completion.
/// </summary>
public static class AuthEndpoints
{
    public const string RefreshCookieName = "pcrm_refresh";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder routes)
    {
        // Setup — refuses to run after the first successful call.
        routes.MapPost("/api/setup", SetupAsync)
              .WithTags("Auth")
              .AllowAnonymous();

        // Auth — public.
        var auth = routes.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        auth.MapPost("/login",  LoginAsync);
        auth.MapPost("/refresh", RefreshAsync);
        auth.MapPost("/logout", LogoutAsync);

        return routes;
    }

    // ---- Setup ------------------------------------------------------------

    private static async Task<IResult> SetupAsync(
        SetupRequest request,
        AppDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return Results.Conflict(new ApiError("setup_already_complete",
                "The setup wizard has already been completed."));
        }

        if (string.IsNullOrWhiteSpace(request.Email)
            || string.IsNullOrWhiteSpace(request.Password)
            || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return Results.BadRequest(new ApiError("validation_failed",
                "Email, password, and display name are required."));
        }

        var pwdCheck = ValidatePassword(request.Password, request.Email);
        if (pwdCheck is not null) return Results.BadRequest(pwdCheck.Value);

        var now      = DateTimeOffset.UtcNow;
        var userId   = Guid.NewGuid();
        var wsId     = Guid.NewGuid();

        var user = new User
        {
            Id              = userId,
            Email           = request.Email.Trim().ToLowerInvariant(),
            PasswordHash    = hasher.Hash(request.Password),
            DisplayName     = request.DisplayName.Trim(),
            IsInstanceAdmin = true,
            IsEmailVerified = true,        // first user is trusted; verify flow lands in v0.2
            CreatedAt       = now
        };

        var workspace = new Workspace
        {
            Id         = wsId,
            Name       = "Personal",
            IsPersonal = true,
            CreatedAt  = now
        };

        var member = new WorkspaceMember
        {
            WorkspaceId = wsId,
            UserId      = userId,
            Role        = WorkspaceRole.Owner,
            JoinedAt    = now
        };

        var instance = new Instance
        {
            SiteName         = string.IsNullOrWhiteSpace(request.SiteName) ? "Personal CRM" : request.SiteName.Trim(),
            DefaultLanguage  = "en-US",
            CreatedAt        = now,
            SetupCompletedAt = now
        };

        db.Users.Add(user);
        db.Workspaces.Add(workspace);
        db.WorkspaceMembers.Add(member);
        db.Instances.Add(instance);

        await db.SaveChangesAsync(ct);

        // Issue the same auth envelope as /login would.
        IssueAuthCookies(http, userId);

        return Results.Ok(new SetupResponse(userId, wsId));
    }

    // ---- Login ------------------------------------------------------------

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        AppDbContext db,
        HttpContext http,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Json(
                new ApiError("invalid_credentials", "Invalid email or password."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var email = request.Email.Trim().ToLowerInvariant();

        var row = await db.Users
            .Where(u => u.Email == email)
            .Select(u => new { u.Id, u.PasswordHash })
            .FirstOrDefaultAsync(ct);

        // Constant-time-ish: hash a dummy even on miss to even out the timing.
        var stored = row?.PasswordHash
                     ?? "$argon2id$v=19$m=65536,t=3,p=4$AAAAAAAAAAAAAAAAAAAAAA$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        var ok     = hasher.Verify(request.Password, stored);

        if (row is null || !ok)
        {
            return Results.Json(
                new ApiError("invalid_credentials", "Invalid email or password."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        IssueAuthCookies(http, row.Id);
        return Results.Ok(new LoginResponse(IssueAccessToken(row.Id), DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    private static IResult RefreshAsync(HttpContext http)
    {
        // Refresh-token rotation lives in v0.2 (along with the JWT signing
        // pipeline). For v0.1, refresh is a no-op that re-issues an access
        // token if the cookie is present and parses.
        if (!http.Request.Cookies.TryGetValue(RefreshCookieName, out var refresh)
            || !Guid.TryParse(refresh, out var userId))
        {
            return Results.Json(
                new ApiError("invalid_credentials", "Refresh token missing or invalid."),
                statusCode: StatusCodes.Status401Unauthorized);
        }
        return Results.Ok(new LoginResponse(IssueAccessToken(userId), DateTimeOffset.UtcNow.AddMinutes(15)));
    }

    private static IResult LogoutAsync(HttpContext http)
    {
        http.Response.Cookies.Delete(RefreshCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Lax,
            Path     = "/"
        });
        return Results.NoContent();
    }

    // ---- Helpers ----------------------------------------------------------

    /// <summary>
    /// Stub access-token issuer. Replaced in v0.2 by the full JWT pipeline
    /// (signing key from config, claims with workspace roles, etc.).
    /// For v0.1 the "token" is just the user-id GUID — enough to wire the
    /// test harness and exercise the auth handler.
    /// </summary>
    private static string IssueAccessToken(Guid userId) => userId.ToString();

    private static void IssueAuthCookies(HttpContext http, Guid userId)
    {
        http.Response.Cookies.Append(RefreshCookieName, userId.ToString(), new CookieOptions
        {
            HttpOnly = true,
            Secure   = true,
            SameSite = SameSiteMode.Lax,
            Path     = "/",
            Expires  = DateTimeOffset.UtcNow.AddDays(30),
            IsEssential = true
        });
    }

    private static ApiError? ValidatePassword(string password, string email)
    {
        if (password.Length < 12)
        {
            return new ApiError("weak_password", "Password must be at least 12 characters.");
        }

        var local = email.Split('@', 2)[0];
        if (local.Length >= 4 && password.Contains(local, StringComparison.OrdinalIgnoreCase))
        {
            return new ApiError("weak_password",
                "Password must not contain the local part of the email address.");
        }

        return null;
    }
}

// ---- DTOs (kept local to avoid bloating Shared) -----------------------------

public record SetupRequest(string Email, string Password, string DisplayName, string? SiteName);
public record SetupResponse(Guid UserId, Guid WorkspaceId);
public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);
