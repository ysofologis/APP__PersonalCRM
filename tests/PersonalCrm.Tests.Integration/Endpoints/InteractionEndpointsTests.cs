using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PersonalCrm.Shared.Dtos;
using PersonalCrm.Tests.Integration.Infrastructure;
using Xunit;

namespace PersonalCrm.Tests.Integration.Endpoints;

public class InteractionEndpointsTests : IClassFixture<CrmWebAppFactory>
{
    private readonly CrmWebAppFactory _factory;

    public InteractionEndpointsTests(CrmWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<(Guid UserId, Guid WorkspaceId, Guid ContactId, HttpClient Client)> BootstrapAsync()
    {
        var (userId, wsId) = await _factory.SeedAsync();
        var client         = _factory.CreateAuthenticatedClient(userId);

        var contactResp = await client.PostAsJsonAsync($"/api/workspaces/{wsId}/contacts", new
        {
            firstName   = "Ada",
            middleName  = (string?)null,
            lastName    = "Lovelace",
            pronouns    = (string?)null,
            birthday    = (DateOnly?)new DateOnly(1815, 12, 10),
            anniversary = (DateOnly?)null,
            cadenceDays = (int?)42
        });
        contactResp.EnsureSuccessStatusCode();
        var contact = await contactResp.Content.ReadFromJsonAsync<ContactDto>();
        return (userId, wsId, contact!.Id, client);
    }

    [Fact]
    public async Task Create_then_list_returns_interaction_on_timeline()
    {
        var (userId, wsId, contactId, client) = await BootstrapAsync();

        var createReq = new
        {
            clientGeneratedId     = Guid.NewGuid(),
            kind                  = InteractionKindDto.Call,
            direction             = InteractionDirectionDto.Outbound,
            occurredAt            = DateTimeOffset.UtcNow.AddDays(-2),
            durationMinutes       = (int?)18,
            location              = "Athens",
            summary               = "Discussed v0.1 plan.",
            participantContactIds = new[] { contactId }
        };

        var createResp = await client.PostAsJsonAsync($"/api/workspaces/{wsId}/interactions", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResp.Content.ReadFromJsonAsync<InteractionDto>();
        created!.ParticipantContactIds.Should().Contain(contactId);
        created.Summary.Should().Be("Discussed v0.1 plan.");

        var listResp = await client.GetAsync($"/api/workspaces/{wsId}/contacts/{contactId}/interactions");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await listResp.Content.ReadAsStringAsync();
        body.Should().Contain(created.Id.ToString());
        body.Should().Contain("Discussed v0.1 plan.");
    }

    [Fact]
    public async Task Create_is_idempotent_on_client_generated_id()
    {
        var (userId, wsId, contactId, client) = await BootstrapAsync();

        var clientGeneratedId = Guid.NewGuid();
        var req = new
        {
            clientGeneratedId,
            kind                  = InteractionKindDto.Message,
            direction             = InteractionDirectionDto.Inbound,
            occurredAt            = DateTimeOffset.UtcNow,
            durationMinutes       = (int?)null,
            location              = (string?)null,
            summary               = "first write",
            participantContactIds = new[] { contactId }
        };

        var first  = await client.PostAsJsonAsync($"/api/workspaces/{wsId}/interactions", req);
        var second = await client.PostAsJsonAsync($"/api/workspaces/{wsId}/interactions", req);

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstDto  = await first.Content.ReadFromJsonAsync<InteractionDto>();
        var secondDto = await second.Content.ReadFromJsonAsync<InteractionDto>();

        // Same row returned for both — no duplicate inserted.
        firstDto!.Id.Should().Be(secondDto!.Id);
    }

    [Fact]
    public async Task Create_with_unknown_participant_returns_400()
    {
        var (userId, wsId, _, client) = await BootstrapAsync();

        var req = new
        {
            clientGeneratedId     = Guid.NewGuid(),
            kind                  = InteractionKindDto.Email,
            direction             = InteractionDirectionDto.Outbound,
            occurredAt            = DateTimeOffset.UtcNow,
            durationMinutes       = (int?)null,
            location              = (string?)null,
            summary               = (string?)null,
            participantContactIds = new[] { Guid.NewGuid() }     // not in this workspace
        };

        var resp = await client.PostAsJsonAsync($"/api/workspaces/{wsId}/interactions", req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var err = await resp.Content.ReadFromJsonAsync<ApiError>();
        err!.Code.Should().Be("validation_failed");
    }

    [Fact]
    public async Task Create_without_participants_returns_400()
    {
        var (userId, wsId, _, client) = await BootstrapAsync();

        var req = new
        {
            clientGeneratedId     = Guid.NewGuid(),
            kind                  = InteractionKindDto.Email,
            direction             = InteractionDirectionDto.Outbound,
            occurredAt            = DateTimeOffset.UtcNow,
            durationMinutes       = (int?)null,
            location              = (string?)null,
            summary               = (string?)null,
            participantContactIds = Array.Empty<Guid>()
        };

        var resp = await client.PostAsJsonAsync($"/api/workspaces/{wsId}/interactions", req);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_for_unknown_contact_returns_404()
    {
        var (userId, wsId, _, client) = await BootstrapAsync();

        var resp = await client.GetAsync($"/api/workspaces/{wsId}/contacts/{Guid.NewGuid()}/interactions");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Non_member_cannot_create_interaction()
    {
        var (_, wsId, contactId, _) = await BootstrapAsync();
        var intruder                 = _factory.CreateAuthenticatedClient(Guid.NewGuid());

        var resp = await intruder.PostAsJsonAsync($"/api/workspaces/{wsId}/interactions", new
        {
            clientGeneratedId     = Guid.NewGuid(),
            kind                  = InteractionKindDto.Call,
            direction             = InteractionDirectionDto.Outbound,
            occurredAt            = DateTimeOffset.UtcNow,
            durationMinutes       = (int?)null,
            location              = (string?)null,
            summary               = (string?)null,
            participantContactIds = new[] { contactId }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
