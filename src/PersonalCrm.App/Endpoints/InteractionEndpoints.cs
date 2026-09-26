using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Infrastructure.Persistence.WorkspaceContext;
using PersonalCrm.Shared.Dtos;

namespace PersonalCrm.App.Endpoints;

/// <summary>
/// Interactions (timeline) endpoints. Idempotent on
/// <c>clientGeneratedId</c> so offline writes can be replayed safely.
/// </summary>
public static class InteractionEndpoints
{
    public static IEndpointRouteBuilder MapInteractionEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/workspaces/{workspaceId:guid}")
                          .RequireAuthorization()
                          .WithTags("Interactions");

        group.MapPost("interactions",                                          CreateInteraction);
        group.MapGet("contacts/{contactId:guid}/interactions",                 ListForContact);

        return routes;
    }

    // ---- handlers ---------------------------------------------------------

    private static async Task<IResult> CreateInteraction(
        Guid workspaceId,
        CreateInteractionRequest request,
        AppDbContext db,
        IWorkspaceContext ctx,
        CancellationToken ct)
    {
        if (!ctx.CanWrite(workspaceId, WorkspaceRole.Editor))
        {
            return Results.Forbid();
        }

        if (request.ParticipantContactIds.Count == 0)
        {
            return Results.BadRequest(new ApiError("validation_failed",
                "At least one participant contact id is required."));
        }

        // Idempotency: if a row with the same clientGeneratedId already
        // exists in this workspace, return it instead of inserting a duplicate.
        var existing = await db.Interactions
            .FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId
                                       && i.ClientGeneratedId == request.ClientGeneratedId,
                                    ct);

        if (existing is not null)
        {
            var existingDto = await BuildDtoAsync(db, existing, ct);
            return Results.CreatedAtRoute("GetContact", new { workspaceId, contactId = Guid.Empty }, existingDto);
        }

        // Validate the participants belong to the workspace.
        var participants = await db.Contacts
            .Where(c => c.WorkspaceId == workspaceId
                        && request.ParticipantContactIds.Contains(c.Id)
                        && c.DeletedAt == null)
            .Select(c => c.Id)
            .ToListAsync(ct);

        if (participants.Count != request.ParticipantContactIds.Count)
        {
            return Results.BadRequest(new ApiError("validation_failed",
                "One or more participantContactIds do not exist in this workspace."));
        }

        var now    = DateTimeOffset.UtcNow;
        var entity = new Interaction
        {
            Id                = Guid.NewGuid(),
            WorkspaceId       = workspaceId,
            ClientGeneratedId = request.ClientGeneratedId,
            Kind              = (InteractionKind)(int)request.Kind,
            Direction         = (InteractionDirection)(int)request.Direction,
            OccurredAt        = request.OccurredAt,
            DurationMinutes   = request.DurationMinutes,
            Location          = request.Location,
            Summary           = request.Summary,
            Version           = 0,
            CreatedAt         = now,
            UpdatedAt         = now,
            Participants      = participants
                .Select(id => new InteractionContact { ContactId = id })
                .ToList()
        };

        db.Interactions.Add(entity);
        await db.SaveChangesAsync(ct);

        var dto = await BuildDtoAsync(db, entity, ct);
        return Results.Created($"/api/workspaces/{workspaceId}/interactions/{entity.Id}", dto);
    }

    private static async Task<IResult> ListForContact(
        Guid workspaceId,
        Guid contactId,
        AppDbContext db,
        IWorkspaceContext ctx,
        CancellationToken ct)
    {
        if (!ctx.CanRead(workspaceId)) return Results.Forbid();

        // Confirm the contact exists in the workspace (404, not empty list).
        var exists = await db.Contacts
            .AnyAsync(c => c.Id == contactId && c.WorkspaceId == workspaceId && c.DeletedAt == null, ct);

        if (!exists) return Results.NotFound();

        var limit = 100;
        var rows  = await db.Interactions
            .AsNoTracking()
            .Where(i => i.WorkspaceId == workspaceId
                        && i.Participants.Any(p => p.ContactId == contactId))
            .OrderByDescending(i => i.OccurredAt)
            .Take(limit)
            .ToListAsync(ct);

        var dtos = new List<InteractionDto>(rows.Count);
        foreach (var row in rows) dtos.Add(await BuildDtoAsync(db, row, ct));

        return Results.Ok(new { interactions = dtos, nextCursor = (DateTimeOffset?)null });
    }

    // ---- mapping -----------------------------------------------------------

    private static async Task<InteractionDto> BuildDtoAsync(
        AppDbContext db,
        Interaction entity,
        CancellationToken ct)
    {
        var participants = entity.Participants.Count > 0
            ? entity.Participants.Select(p => p.ContactId).ToList()
            : await db.InteractionContacts
                .Where(ic => ic.InteractionId == entity.Id)
                .Select(ic => ic.ContactId)
                .ToListAsync(ct);

        return new InteractionDto(
            entity.Id,
            entity.WorkspaceId,
            entity.ClientGeneratedId,
            (InteractionKindDto)(int)entity.Kind,
            (InteractionDirectionDto)(int)entity.Direction,
            entity.OccurredAt,
            entity.DurationMinutes,
            entity.Location,
            entity.Summary,
            entity.Version,
            participants,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
