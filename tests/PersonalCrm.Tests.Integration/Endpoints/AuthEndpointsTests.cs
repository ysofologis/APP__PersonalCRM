using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Tests.Integration.Infrastructure;
using Xunit;

namespace PersonalCrm.Tests.Integration.Endpoints;

/// <summary>
/// Exercises the v0.1 auth surface documented in <c>docs/V0.1_API.md</c>:
/// setup, login, refresh, logout, plus the error envelopes.
/// </summary>
public class AuthEndpointsTests : IClassFixture<CrmWebAppFactory>
{
    private readonly CrmWebAppFactory _factory;

    public AuthEndpointsTests(CrmWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Setup_creates_admin_and_personal_workspace()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/setup", new
        {
            email       = "ada@example.com",
            password    = "correct-horse-battery-staple",
            displayName = "Ada Lovelace",
            siteName    = "Lovelace CRM"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<SetupResponse>();
        body.Should().NotBeNull();
        body!.UserId.Should().NotBeEmpty();
        body.WorkspaceId.Should().NotBeEmpty();

        // The setup endpoint also issues a refresh cookie.
        resp.Headers.Should().Contain(h => h.Key == "Set-Cookie"
            && h.Value.Any(v => v.StartsWith("pcrm_refresh=")));

        // The DB now has the user, the workspace, the membership, and the Instance row.
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var user = await db.Users.FirstAsync(u => u.Email == "ada@example.com");
        user.IsInstanceAdmin.Should().BeTrue();

        var member = await db.WorkspaceMembers
            .FirstAsync(m => m.UserId == user.Id && m.WorkspaceId == body.WorkspaceId);
        member.Role.Should().Be(WorkspaceRole.Owner);

        var instance = await db.Instances.FirstAsync();
        instance.SetupCompletedAt.Should().NotBeNull();
        instance.SiteName.Should().Be("Lovelace CRM");
    }

    [Fact]
    public async Task Setup_refuses_when_admin_already_exists()
    {
        var client = _factory.CreateClient();

        // First call succeeds.
        var first = await client.PostAsJsonAsync("/api/setup", new
        {
            email       = "ada@example.com",
            password    = "correct-horse-battery-staple",
            displayName = "Ada Lovelace",
            siteName    = (string?)null
        });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Second call is rejected.
        var second = await client.PostAsJsonAsync("/api/setup", new
        {
            email       = "other@example.com",
            password    = "another-strong-password",
            displayName = "Other User",
            siteName    = (string?)null
        });

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var err = await second.Content.ReadFromJsonAsync<ApiError>();
        err!.Code.Should().Be("setup_already_complete");
    }

    [Fact]
    public async Task Setup_rejects_weak_password()
    {
        var client = _factory.CreateClient();

        var resp = await client.PostAsJsonAsync("/api/setup", new
        {
            email       = "ada@example.com",
            password    = "short",
            displayName = "Ada",
            siteName    = (string?)null
        });

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await resp.Content.ReadFromJsonAsync<ApiError>();
        err!.Code.Should().Be("weak_password");
    }

    [Fact]
    public async Task Login_returns_token_and_refresh_cookie()
    {
        // Seed via the wizard so the password is real.
        await _factory.CreateClient().PostAsJsonAsync("/api/setup", new
        {
            email       = "ada@example.com",
            password    = "correct-horse-battery-staple",
            displayName = "Ada",
            siteName    = (string?)null
        });

        var loginClient = _factory.CreateClient();
        var resp = await loginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "ada@example.com",
            password = "correct-horse-battery-staple"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<LoginResponse>();
        body!.AccessToken.Should().NotBeNullOrEmpty();
        body.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);

        resp.Headers.Should().Contain(h => h.Key == "Set-Cookie"
            && h.Value.Any(v => v.StartsWith("pcrm_refresh=")));
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401()
    {
        await _factory.CreateClient().PostAsJsonAsync("/api/setup", new
        {
            email       = "ada@example.com",
            password    = "correct-horse-battery-staple",
            displayName = "Ada",
            siteName    = (string?)null
        });

        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "ada@example.com",
            password = "wrong-password-999"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var err = await resp.Content.ReadFromJsonAsync<ApiError>();
        err!.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Login_with_unknown_email_returns_same_error_as_wrong_password()
    {
        var client = _factory.CreateClient();
        var resp   = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "ghost@example.com",
            password = "any-password-at-all"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        var err = await resp.Content.ReadFromJsonAsync<ApiError>();
        err!.Code.Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Refresh_without_cookie_returns_401()
    {
        var client = _factory.CreateClient();
        var resp   = await client.PostAsync("/api/auth/refresh", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_clears_the_refresh_cookie()
    {
        // Set up + log in.
        var setupClient = _factory.CreateClient();
        await setupClient.PostAsJsonAsync("/api/setup", new
        {
            email       = "ada@example.com",
            password    = "correct-horse-battery-staple",
            displayName = "Ada",
            siteName    = (string?)null
        });
        await setupClient.PostAsJsonAsync("/api/auth/login", new
        {
            email    = "ada@example.com",
            password = "correct-horse-battery-staple"
        });

        var logout = await setupClient.PostAsync("/api/auth/logout", content: null);

        logout.StatusCode.Should().Be(HttpStatusCode.NoContent);
        logout.Headers.Should().Contain(h => h.Key == "Set-Cookie"
            && h.Value.Any(v => v.StartsWith("pcrm_refresh=") && v.Contains("max-age=0")));
    }
}
