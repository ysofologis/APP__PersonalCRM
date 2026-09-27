namespace PersonalCrm.Contracts.Dtos;

/// <summary>
/// Wire-shape for a <c>Contact</c> shared between Server (API response) and
/// WASM (IndexedDB cache + UI). Kept deliberately narrow — fields not needed
/// by the client (e.g. <c>Version</c> for optimistic concurrency) live in
/// a separate <c>ContactDetailDto</c> when the client actually needs them.
/// </summary>
public record ContactDto(
    Guid Id,
    Guid WorkspaceId,
    string FirstName,
    string? MiddleName,
    string LastName,
    string FullName,
    string? Pronouns,
    string? PhotoUrl,
    DateOnly? Birthday,
    DateOnly? Anniversary,
    int? CadenceDays,
    IReadOnlyList<string> Circles,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Payload for creating a contact.</summary>
public record CreateContactRequest(
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Pronouns,
    DateOnly? Birthday,
    DateOnly? Anniversary,
    int? CadenceDays);

/// <summary>Payload for updating a contact. <c>ExpectedVersion</c> drives optimistic concurrency.</summary>
public record UpdateContactRequest(
    Guid Id,
    int ExpectedVersion,
    string FirstName,
    string? MiddleName,
    string LastName,
    string? Pronouns,
    DateOnly? Birthday,
    DateOnly? Anniversary,
    int? CadenceDays);
