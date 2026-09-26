using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Contracts.Dtos;
using PersonalCrm.Tests.Integration.Infrastructure;
using Xunit;

namespace PersonalCrm.Tests.Integration.Endpoints;

public class ContactEndpointsTests : IClassFixture<CrmWebAppFactory>
{
    private readonly CrmWebAppFactory _factory;

    public ContactEndpointsTests(CrmWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_request_is_rejected()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db       = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wsId     = (await db.Workspaces.FirstAsync()).Id;

        // No X-Test-User-Id header.
        var client = _factory.CreateClient();
        var resp   = await client.GetAsync($"/api/workspaces/{wsId}/contacts");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Member_can_list_create_get_update_and_delete_contact()
    {
        var (userId, wsId) = await _factory.SeedAsync();
        var client         = _factory.CreateAuthenticatedClient(userId);

        // LIST (empty initially)
        var listResp = await client.GetAsync($"/api/workspaces/{wsId}/contacts");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var initial = await listResp.Content.ReadFromJsonAsync<List<ContactDto>>();
        initial.Should().NotBeNull().And.BeEmpty();

        // CREATE
        var createReq = new CreateContactRequest(
            FirstName:   "Ada",
            MiddleName:  null,
            LastName:    "Lovelace",
            Pronouns:    null,
            Birthday:    new DateOnly(1815, 12, 10),
            Anniversary: null,
            CadenceDays: 42);

        var createResp = await client.PostAsJsonAsync($"/api/workspaces/{wsId}/contacts", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<ContactDto>();
        created.Should().NotBeNull();
        created!.FirstName.Should().Be("Ada");
        created.LastName.Should().Be("Lovelace");
        created.WorkspaceId.Should().Be(wsId);

        // GET
        var getResp = await client.GetAsync($"/api/workspaces/{wsId}/contacts/{created.Id}");
        getResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await getResp.Content.ReadFromJsonAsync<ContactDto>();
        fetched!.Id.Should().Be(created.Id);
        fetched.Birthday.Should().Be(new DateOnly(1815, 12, 10));

        // UPDATE (with correct version)
        var updateReq = new UpdateContactRequest(
            Id:              created.Id,
            ExpectedVersion: 0,
            FirstName:       "Augusta Ada",
            MiddleName:      null,
            LastName:        "Lovelace",
            Pronouns:        "she/her",
            Birthday:        new DateOnly(1815, 12, 10),
            Anniversary:     null,
            CadenceDays:     30);

        var updateResp = await client.PatchAsync(
            $"/api/workspaces/{wsId}/contacts/{created.Id}",
            JsonContent.Create(updateReq));

        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResp.Content.ReadFromJsonAsync<ContactDto>();
        updated!.FirstName.Should().Be("Augusta Ada");
        updated.Pronouns.Should().Be("she/her");

        // UPDATE with stale version → 409
        var staleReq = updateReq with { ExpectedVersion = 0 };
        var staleResp = await client.PatchAsync(
            $"/api/workspaces/{wsId}/contacts/{created.Id}",
            JsonContent.Create(staleReq));
        staleResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // DELETE
        var deleteResp = await client.DeleteAsync($"/api/workspaces/{wsId}/contacts/{created.Id}");
        deleteResp.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET after delete → 404
        var afterDelete = await client.GetAsync($"/api/workspaces/{wsId}/contacts/{created.Id}");
        afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Non_member_is_forbidden()
    {
        var (_, wsId)        = await _factory.SeedAsync();
        var intruderClient   = _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var resp = await intruderClient.GetAsync($"/api/workspaces/{wsId}/contacts");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_without_first_or_last_name_returns_400()
    {
        var (userId, wsId) = await _factory.SeedAsync();
        var client         = _factory.CreateAuthenticatedClient(userId);

        var bad = new CreateContactRequest(" ", " ", " ", null, null, null, null);

        var resp = await client.PostAsJsonAsync($"/api/workspaces/{wsId}/contacts", bad);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
