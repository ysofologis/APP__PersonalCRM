using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using PersonalCrm.Contracts.Dtos;

namespace PersonalCrm.App.Services;

/// <summary>
/// Thin wrapper around <see cref="HttpClient"/> that:
///   - targets the API endpoints we own (no controller boilerplate on the page side),
///   - attaches the JWT bearer from the currently signed-in user (set after /setup or /login),
///   - surfaces server error envelopes as <see cref="ApiException"/> for friendly UI handling.
///
/// Tokens live in-memory only — v0.1 doesn't have a refresh-token rotation flow on the
/// client. Reload of the page drops the token, which is acceptable for v0.1 since the
/// refresh cookie is httpOnly and the browser will resend it on the next request.
/// </summary>
public sealed class ApiClient
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly NavigationManager  _nav;
    private          Uri?               _baseAddress;

    public string? AccessToken { get; set; }
    public Guid?   UserId      { get; set; }
    public Guid?   WorkspaceId { get; set; }

    public ApiClient(IHttpClientFactory httpFactory, NavigationManager nav)
    {
        _httpFactory = httpFactory;
        _nav         = nav;
    }

    /// <summary>
    /// Resolves <see cref="NavigationManager.BaseUri"/> once and caches it. The
    /// Blazor navigation manager can briefly report <c>about:blank</c> or an
    /// empty URI during prerender, so we resolve lazily and throw a clear
    /// <see cref="InvalidOperationException"/> if the URI is unusable — much
    /// easier to diagnose than a 404 from a malformed request URL.
    /// </summary>
    private Uri ResolveBaseAddress()
    {
        if (_baseAddress is not null) return _baseAddress;

        var raw = _nav.BaseUri;
        if (string.IsNullOrWhiteSpace(raw) || raw == "about:blank")
        {
            throw new InvalidOperationException(
                $"NavigationManager.BaseUri is '{raw}'; cannot resolve an absolute base address for /api/* calls. " +
                "This usually means the component is being prerendered before the circuit is established — " +
                "guard ApiClient calls behind IComponentRenderer's OnAfterRenderAsync or render-mode Interactive.");
        }

        // Always end with a trailing slash so relative URIs like "api/setup"
        // resolve correctly when combined with the base address.
        var normalised = raw.EndsWith('/') ? raw : raw + "/";
        _baseAddress   = new Uri(normalised);
        return _baseAddress;
    }

    /// <summary>
    /// Builds a per-call <see cref="HttpClient"/> with an absolute base address
    /// (resolved from <see cref="NavigationManager.BaseUri"/>) so /api/* requests
    /// go to the same origin that served the Blazor app — works behind reverse
    /// proxies, custom ports, and HTTPS terminators without hard-coding a host.
    /// </summary>
    private HttpClient BuildClient()
    {
        var http = _httpFactory.CreateClient();
        if (http.BaseAddress is null)
        {
            http.BaseAddress = ResolveBaseAddress();
        }
        return http;
    }

    /// <summary>One-shot bootstrap endpoint. Refuses to run after the first success.</summary>
    public async Task<SetupResponse> SetupAsync(SetupRequest req, CancellationToken ct = default)
    {
        using var http = BuildClient();
        var resp = await http.PostAsJsonAsync("/api/setup", req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw await ToExceptionAsync(resp, "/api/setup", ct);
        }
        var body = await resp.Content.ReadFromJsonAsync<SetupResponse>(cancellationToken: ct);
        UserId      = body!.UserId;
        WorkspaceId = body.WorkspaceId;
        return body;
    }

    /// <summary>Read-only check for whether the instance has been bootstrapped.</summary>
    public async Task<SetupStatus> GetSetupStatusAsync(CancellationToken ct = default)
    {
        using var http = BuildClient();
        var resp = await http.GetAsync("/api/setup/status", ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw await ToExceptionAsync(resp, "/api/setup/status", ct);
        }
        return (await resp.Content
            .ReadFromJsonAsync<SetupStatus>(cancellationToken: ct))!;
    }

    public async Task<ListContactsResponse> ListContactsAsync(
        Guid workspaceId,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        EnsureAuthed();
        using var http = BuildClient();
        var req  = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/workspaces/{workspaceId}/contacts?limit={limit}&offset={offset}");
        AddAuthHeader(req);
        var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw await ToExceptionAsync(
                resp, $"/api/workspaces/{workspaceId}/contacts?limit={limit}&offset={offset}", ct);
        }
        return (await resp.Content
            .ReadFromJsonAsync<ListContactsResponse>(cancellationToken: ct))!;
    }

    private void EnsureAuthed()
    {
        if (string.IsNullOrEmpty(AccessToken))
        {
            throw new ApiException(
                "not_authenticated",
                "Complete the setup wizard or sign in first.",
                StatusCodes.Status401Unauthorized);
        }
    }

    private void AddAuthHeader(HttpRequestMessage req)
    {
        if (!string.IsNullOrEmpty(AccessToken))
        {
            req.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", AccessToken);
        }
    }

    private static async Task<ApiException> ToExceptionAsync(
        HttpResponseMessage resp,
        string              path,
        CancellationToken   ct)
    {
        try
        {
            var err = await resp.Content
                .ReadFromJsonAsync<ApiError>(cancellationToken: ct);
            if (err is not null)
            {
                return new ApiException(err.Code, err.Message, (int)resp.StatusCode, path);
            }
        }
        catch
        {
            // Fall through to the generic envelope below.
        }

        // Generic envelope: include the path so the failure message points at
        // the exact endpoint that was attempted (e.g. when the response body
        // is empty or HTML rather than the structured ApiError envelope).
        return new ApiException(
            "http_error",
            $"Request failed with status {(int)resp.StatusCode} for {path}.",
            (int)resp.StatusCode,
            path);
    }
}

public sealed class ApiException : Exception
{
    public string  Code       { get; }
    public int     StatusCode { get; }
    public string? Path       { get; }

    public ApiException(string code, string message, int statusCode)
        : this(code, message, statusCode, path: null)
    {
    }

    public ApiException(string code, string message, int statusCode, string? path)
        : base(message)
    {
        Code       = code;
        StatusCode = statusCode;
        Path       = path;
    }
}
