namespace PersonalCrm.Contracts.Dtos;

/// <summary>
/// Uniform JSON error envelope for every API endpoint.
/// </summary>
public record ApiError(string Code, string Message);

// ---- Setup ---------------------------------------------------------------

public record SetupRequest(string Email, string Password, string DisplayName, string? SiteName);
public record SetupResponse(Guid UserId, Guid WorkspaceId);

/// <summary>
/// Response from <c>GET /api/setup/status</c>. Used by the /setup wizard
/// to detect a pre-bootstrapped instance and redirect to /login instead.
/// </summary>
public record SetupStatus(bool Bootstrapped, string? SiteName);

// ---- Auth ----------------------------------------------------------------

public record LoginRequest(string Email, string Password);
public record LoginResponse(string AccessToken, DateTimeOffset ExpiresAt);

// ---- Contacts ------------------------------------------------------------

/// <summary>
/// Paginated contact list. Returned as a record so the Blazor client can
/// deserialise it without pulling the full Contracts.Dtos namespace surface.
/// </summary>
public record ListContactsResponse(
    IReadOnlyList<ContactDto> Contacts,
    int? NextOffset);

// ---- Interactions (kept thin here; richer DTOs in InteractionDto.cs) ------

public record ListInteractionsResponse(
    IReadOnlyList<InteractionDto> Interactions,
    DateTimeOffset? NextCursor);
