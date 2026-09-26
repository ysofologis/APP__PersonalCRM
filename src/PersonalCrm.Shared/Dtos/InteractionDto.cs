namespace PersonalCrm.Shared.Dtos;

public enum InteractionKindDto
{
    Call    = 0,
    Meeting = 1,
    Email   = 2,
    Message = 3,
    Gift    = 4,
    Event   = 5,
    Other   = 99
}

public enum InteractionDirectionDto
{
    Outbound = 0,
    Inbound  = 1,
    Neutral  = 2
}

public record InteractionDto(
    Guid Id,
    Guid WorkspaceId,
    Guid ClientGeneratedId,
    InteractionKindDto Kind,
    InteractionDirectionDto Direction,
    DateTimeOffset OccurredAt,
    int? DurationMinutes,
    string? Location,
    string? Summary,
    int Version,
    IReadOnlyList<Guid> ParticipantContactIds,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public record CreateInteractionRequest(
    Guid ClientGeneratedId,
    InteractionKindDto Kind,
    InteractionDirectionDto Direction,
    DateTimeOffset OccurredAt,
    int? DurationMinutes,
    string? Location,
    string? Summary,
    IReadOnlyList<Guid> ParticipantContactIds);
