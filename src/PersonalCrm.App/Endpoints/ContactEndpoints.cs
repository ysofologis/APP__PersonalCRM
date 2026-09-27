using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Infrastructure.Persistence.WorkspaceContext;
using PersonalCrm.Contracts.Dtos;

namespace PersonalCrm.App.Endpoints;

/// <summary>
/// First vertical slice of the API surface: contacts inside a workspace.
/// All endpoints require authentication (a 401 with no body for anonymous)
/// and workspace membership (a 403 with a JSON error otherwise).
///
/// Endpoints are declared as <see cref="IEndpointRouteBuilder"/> extensions
/// so they can be composed with other feature groups inside
/// <c>Program.cs</c>.
/// </summary>
public static class ContactEndpoints
{
    public static IEndpointRouteBuilder MapContactEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/workspaces/{workspaceId:guid}/contacts")
                          .RequireAuthorization()
                          .WithTags("Contacts");

        group.MapGet("",              ListContacts);
        group.MapGet("{contactId:guid}", GetContact).WithName("GetContact");
        group.MapPost("",             CreateContact);
        group.MapPatch("{contactId:guid}", UpdateContact);
        group.MapDelete("{contactId:guid}", DeleteContact);

        return routes;
    }

    // ---- handlers ---------------------------------------------------------

    private static async Task<Results<
        Ok<ListContactsResponse>,
        ForbidHttpResult>> ListContacts(
        Guid workspaceId,
        AppDbContext db,
        IWorkspaceContext ctx,
        CancellationToken ct)
    {
        if (!ctx.CanRead(workspaceId)) return TypedResults.Forbid();

        var rows = await db.Contacts
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId && c.DeletedAt == null)
            .OrderBy(c => c.LastName).ThenBy(c => c.FirstName)
            .Take(200)
            .Select(c => new ContactDto(
                c.Id, c.WorkspaceId, c.FirstName, c.MiddleName, c.LastName,
                c.FirstName + " " + c.LastName,   // FullName — server-side fallback
                c.Pronouns, c.PhotoUrl, c.Birthday, c.Anniversary, c.CadenceDays,
                Array.Empty<string>(),              // circles (filled by separate endpoint)
                Array.Empty<string>(),              // tags    (filled by separate endpoint)
                c.CreatedAt, c.UpdatedAt))
            .ToListAsync(ct);

        return TypedResults.Ok(new ListContactsResponse(rows, NextOffset: null));
    }

    private static async Task<Results<Ok<ContactDto>, NotFound, ForbidHttpResult>> GetContact(
        Guid workspaceId,
        Guid contactId,
        AppDbContext db,
        IWorkspaceContext ctx,
        CancellationToken ct)
    {
        if (!ctx.CanRead(workspaceId)) return TypedResults.Forbid();

        var contact = await db.Contacts
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == contactId && c.WorkspaceId == workspaceId, ct);

        if (contact is null || contact.DeletedAt is not null) return TypedResults.NotFound();

        return TypedResults.Ok(MapToDto(contact));
    }

    private static async Task<Results<
        
        CreatedAtRoute<ContactDto>,
        BadRequest<ApiError>,
        ForbidHttpResult>> CreateContact(
        Guid workspaceId,
        CreateContactRequest request,
        AppDbContext db,
        IWorkspaceContext ctx,
        CancellationToken ct)
    {
        if (!ctx.CanWrite(workspaceId, WorkspaceRole.Editor)) return TypedResults.Forbid();
        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return TypedResults.BadRequest(new ApiError("first_name_and_last_name_required",
                "First name and last name are required."));
        }

        var now    = DateTimeOffset.UtcNow;
        var entity = new Contact
        {
            Id          = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            FirstName   = request.FirstName.Trim(),
            MiddleName  = request.MiddleName?.Trim(),
            LastName    = request.LastName.Trim(),
            Pronouns    = request.Pronouns?.Trim(),
            Birthday    = request.Birthday,
            Anniversary = request.Anniversary,
            CadenceDays = request.CadenceDays,
            Version     = 0,
            CreatedAt   = now,
            UpdatedAt   = now
        };

        db.Contacts.Add(entity);
        await db.SaveChangesAsync(ct);

        return TypedResults.CreatedAtRoute(
            MapToDto(entity),
            "GetContact",
            new { workspaceId, contactId = entity.Id });
    }

    private static async Task<Results<
        
        Ok<ContactDto>,
        BadRequest<ApiError>,
        NotFound,
        Conflict<ApiError>,
        ForbidHttpResult>> UpdateContact(
        Guid workspaceId,
        Guid contactId,
        UpdateContactRequest request,
        AppDbContext db,
        IWorkspaceContext ctx,
        CancellationToken ct)
    {
        if (!ctx.CanWrite(workspaceId, WorkspaceRole.Editor)) return TypedResults.Forbid();

        var entity = await db.Contacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.WorkspaceId == workspaceId, ct);

        if (entity is null || entity.DeletedAt is not null) return TypedResults.NotFound();

        if (entity.Version != request.ExpectedVersion)
        {
            return TypedResults.Conflict(new ApiError(
                "stale_version",
                $"Expected version {request.ExpectedVersion}, found {entity.Version}. Refresh and retry."));
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return TypedResults.BadRequest(new ApiError("first_name_and_last_name_required",
                "First name and last name are required."));
        }

        entity.FirstName   = request.FirstName.Trim();
        entity.MiddleName  = request.MiddleName?.Trim();
        entity.LastName    = request.LastName.Trim();
        entity.Pronouns    = request.Pronouns?.Trim();
        entity.Birthday    = request.Birthday;
        entity.Anniversary = request.Anniversary;
        entity.CadenceDays = request.CadenceDays;
        entity.Version     = entity.Version + 1;
        entity.UpdatedAt   = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return TypedResults.Ok(MapToDto(entity));
    }

    private static async Task<Results<NoContent, NotFound, ForbidHttpResult>> DeleteContact(
        Guid workspaceId,
        Guid contactId,
        AppDbContext db,
        IWorkspaceContext ctx,
        CancellationToken ct)
    {
        if (!ctx.CanWrite(workspaceId, WorkspaceRole.Editor)) return TypedResults.Forbid();

        var entity = await db.Contacts
            .FirstOrDefaultAsync(c => c.Id == contactId && c.WorkspaceId == workspaceId, ct);

        if (entity is null) return TypedResults.NotFound();

        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.Version   = entity.Version + 1;
        await db.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }

    // ---- mapping -----------------------------------------------------------

    private static ContactDto MapToDto(Contact c) =>
        new(
            c.Id, c.WorkspaceId, c.FirstName, c.MiddleName, c.LastName,
            c.FirstName + " " + c.LastName,
            c.Pronouns, c.PhotoUrl, c.Birthday, c.Anniversary, c.CadenceDays,
            Array.Empty<string>(),
            Array.Empty<string>(),
            c.CreatedAt, c.UpdatedAt);
}

/// <summary>
/// Uniform JSON error envelope for all API endpoints — now defined in
/// <c>PersonalCrm.Contracts.Dtos.ApiError</c> so the Blazor client can
/// deserialise server errors without referencing the App layer.
/// </summary>
