using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Didww.Api3.Tests;

public class SmsTrunkGroupTest : BaseTest
{
    private const string GroupId = "2b3c4d5e-0001-4b6c-9d7e-8f9a0b1c2d01";
    private const string HttpInTrunkId = "1a2b3c4d-0002-4a5b-8c6d-7e8f9a0b1c02";
    private const string SmscTrunkId = "1a2b3c4d-0005-4a5b-8c6d-7e8f9a0b1c05";

    [Fact]
    public async Task TestListSmsTrunkGroups()
    {
        StubGet("sms_trunk_groups", "sms_trunk_groups/index.json");

        var response = await Client.SmsTrunkGroups().ListAsync();
        var groups = response.Data;

        groups.Should().HaveCount(2);
        groups[0].Id.Should().Be(GroupId);
        groups[0].Name.Should().Be("Inbound SMS");
        groups[0].ExternalReferenceId.Should().BeNull();
        groups[0].CreatedAt.Should().Be(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero));
        groups[0].UpdatedAt.Should().Be(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero));
        groups[0].SmsTrunksCount.Should().Be(2);
        groups[1].Name.Should().Be("Outbound SMS");
        groups[1].SmsTrunksCount.Should().Be(0);
    }

    [Fact]
    public async Task TestFindSmsTrunkGroupWithIncludedSmsTrunks()
    {
        StubGet("sms_trunk_groups/" + GroupId, "sms_trunk_groups/show_with_sms_trunks.json");

        var queryParams = new QueryParams().Include("sms_trunks");
        var response = await Client.SmsTrunkGroups().FindAsync(GroupId, queryParams);
        var group = response.Data;

        group.SmsTrunks.Should().HaveCount(2);
        group.SmsTrunks![0].Id.Should().Be(HttpInTrunkId);
        group.SmsTrunks[0].Configuration.Should().BeOfType<HttpInConfiguration>();
        group.SmsTrunks[1].Id.Should().Be(SmscTrunkId);
        group.SmsTrunks[1].Configuration.Should().BeOfType<SmppSmscConfiguration>();
        group.SmsTrunksCount.Should().Be(2);
    }

    [Fact]
    public async Task TestFindSmsTrunkGroupNotFound()
    {
        StubGet("sms_trunk_groups/" + GroupId, "sms_trunk_groups/not_found.json", 404);

        var act = () => Client.SmsTrunkGroups().FindAsync(GroupId);

        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(404);
    }

    [Fact]
    public async Task TestCreateSmsTrunkGroup()
    {
        StubPost("sms_trunk_groups", "sms_trunk_groups/create_request.json", "sms_trunk_groups/create.json");

        var group = new SmsTrunkGroup
        {
            Name = "Inbound SMS",
            SmsTrunks = new List<SmsTrunk> { SmsTrunk.Build(HttpInTrunkId) }
        };

        var created = (await Client.SmsTrunkGroups().CreateAsync(group)).Data;

        created.Id.Should().Be(GroupId);
        created.Name.Should().Be("Inbound SMS");
        created.SmsTrunksCount.Should().Be(1);
    }

    [Fact]
    public async Task TestUpdateSmsTrunkGroupExternalReferenceId()
    {
        StubPatch("sms_trunk_groups/" + GroupId,
            "sms_trunk_groups/update_external_reference_id_request.json",
            "sms_trunk_groups/update_external_reference_id.json");

        var group = SmsTrunkGroup.Build(GroupId);
        group.ExternalReferenceId = "updated-ext-ref-99";

        var updated = (await Client.SmsTrunkGroups().UpdateAsync(group)).Data;

        updated.ExternalReferenceId.Should().Be("updated-ext-ref-99");
    }

    [Fact]
    public async Task TestUpdateSmsTrunkGroupMembership()
    {
        StubPatch("sms_trunk_groups/" + GroupId,
            "sms_trunk_groups/update_sms_trunks_request.json",
            "sms_trunk_groups/update_sms_trunks.json");

        var group = SmsTrunkGroup.Build(GroupId);
        group.SmsTrunks = new List<SmsTrunk> { SmsTrunk.Build(HttpInTrunkId), SmsTrunk.Build(SmscTrunkId) };

        var updated = (await Client.SmsTrunkGroups().UpdateAsync(group)).Data;

        updated.SmsTrunksCount.Should().Be(2);
    }

    [Fact]
    public async Task TestUpdateSmsTrunkGroupClearMembershipSendsEmptyList()
    {
        // Request body: "sms_trunks": {"data": []}.
        StubPatch("sms_trunk_groups/" + GroupId,
            "sms_trunk_groups/update_clear_sms_trunks_request.json",
            "sms_trunk_groups/update_sms_trunks.json");

        var group = SmsTrunkGroup.Build(GroupId);
        group.SmsTrunks = new List<SmsTrunk>();

        await Client.SmsTrunkGroups().UpdateAsync(group);

        var body = RequestsTo("/v3/sms_trunk_groups/" + GroupId).Should().ContainSingle().Subject.Body!;
        JObject.Parse(body).SelectToken("data.relationships.sms_trunks.data")
            .Should().BeOfType<JArray>().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task TestDeleteSmsTrunkGroup()
    {
        StubDelete("sms_trunk_groups/" + GroupId);

        await Client.SmsTrunkGroups().DeleteAsync(GroupId);

        RequestsTo("/v3/sms_trunk_groups/" + GroupId).Should().ContainSingle().Which.Method.Should().Be("DELETE");
    }

    [Fact]
    public async Task TestDeleteSmsTrunkGroupInUse()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/sms_trunk_groups/" + GroupId).UsingDelete()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(LoadFixture("sms_trunk_groups/delete_error.json"))
        );

        var act = () => Client.SmsTrunkGroups().DeleteAsync(GroupId);

        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(422);
    }
}
