using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PersonalCrm.Tests.Integration.Infrastructure;

/// <summary>
/// Test-only authentication handler that trusts an <c>X-Test-User-Id</c>
/// header to mint a <see cref="ClaimsPrincipal"/> for the request. Real
/// authentication is exercised by E2E tests against a running instance.
/// </summary>
public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";
    public const string UserIdHeader = "X-Test-User-Id";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(UserIdHeader, out var userIdHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!Guid.TryParse(userIdHeader.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid user id header."));
        }

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim("sub",                  userId.ToString())
            },
            authenticationType: SchemeName);

        var principal  = new ClaimsPrincipal(identity);
        var ticket     = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
