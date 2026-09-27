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

    public string? AccessToken { get; set; }
    public Guid?   UserId      { get; set; }
    public Guid?   WorkspaceId { get; set; }

    public ApiClient(IHttpClientFactory httpFactory, NavigationManager nav)
    {
        _httpFactory = httpFactory;
        _nav         = nav;
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
            http.BaseAddress = new Uri(_nav.BaseUri);
        }
        return http;
    }

    /// <summary>One-shot bootstrap endpoint. Refuses to run after the first success.</summary>
    public async Task<SetupResponse> SetupAsync(SetupRequest req, CancellationToken ct = default)
    {
        using var http = BuildClient();
        var resp = await http.PostAsJsonAsync("api/setup", req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw await ToExceptionAsync(resp, ct);
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
        var resp = await http.GetAsync("api/setup/status", ct);
        if (!resp.IsSuccessStatusCode)
        {
            throw await ToExceptionAsync(resp, ct);
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
            $"api/workspaces/{workspaceId}/contacts?limit={limit}&offset={offset}");
        AddAuthHeader(req);
        var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode) throw await ToExceptionAsync(resp, ct);
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
        CancellationToken ct)
    {
        try
        {
            var err = await resp.Content
                .ReadFromJsonAsync<ApiError>(cancellationToken: ct);
            if (err is not null)
            {
                return new ApiException(err.Code, err.Message, (int)resp.StatusCode);
            }
        }
        catch
        {
            // Fall through to the generic envelope below.
        }

        return new ApiException(
            "http_error",
            $"Request failed with status {(int)resp.StatusCode}.",
            (int)resp.StatusCode);
    }
}

public sealed class ApiException : Exception
{
    public string  Code       { get; }
    public int     StatusCode { get; }

    public ApiException(string code, string message, int statusCode)
        : base(message)
    {
        Code       = code;
        StatusCode = statusCode;
    }
}
