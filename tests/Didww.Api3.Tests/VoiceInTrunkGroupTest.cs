using Didww.Api3.Http;
using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class VoiceInTrunkGroupTest : BaseTest
{
    [Fact]
    public async Task TestListVoiceInTrunkGroups()
    {
        StubGet("voice_in_trunk_groups", "voice_in_trunk_groups/index.json");

        var queryParams = new QueryParams().Include("voice_in_trunks");
        var response = await Client.VoiceInTrunkGroups().ListAsync(queryParams);
        var groups = response.Data;

        groups.Should().NotBeEmpty();

        var first = groups[0];
        first.Id.Should().Be("837c5764-a6c3-456f-aa37-71fc8f8ca07b");
        first.Name.Should().Be("sample trunk group");
    }

    [Fact]
    public async Task TestCreateVoiceInTrunkGroup()
    {
        StubPost("voice_in_trunk_groups", "voice_in_trunk_groups/create_request.json", "voice_in_trunk_groups/create.json");

        var group = new VoiceInTrunkGroup
        {
            Name = "trunk group sample with 2 trunks",
            CapacityLimit = 1000,
            VoiceInTrunks = new List<VoiceInTrunk>
            {
                VoiceInTrunk.Build("7c15bca2-7f17-46fb-9486-7e2a17158c7e"),
                VoiceInTrunk.Build("b07a4cab-48c6-4b3a-9670-11b90b81bdef")
            }
        };

        var response = await Client.VoiceInTrunkGroups().CreateAsync(group);
        var created = response.Data;

        created.Id.Should().Be("b2319703-ce6c-480d-bb53-614e7abcfc96");
        created.Name.Should().Be("trunk group sample with 2 trunks");
        created.CapacityLimit.Should().Be(1000);
    }

    [Fact]
    public async Task TestUpdateVoiceInTrunkGroup()
    {
        StubPatch("voice_in_trunk_groups/b2319703-ce6c-480d-bb53-614e7abcfc96", "voice_in_trunk_groups/update_request.json", "voice_in_trunk_groups/update.json");

        var group = VoiceInTrunkGroup.Build("b2319703-ce6c-480d-bb53-614e7abcfc96");
        group.Name = "trunk group sample updated with 2 trunks";
        group.CapacityLimit = 500;

        var response = await Client.VoiceInTrunkGroups().UpdateAsync(group);
        var updated = response.Data;

        updated.Id.Should().Be("b2319703-ce6c-480d-bb53-614e7abcfc96");
        updated.Name.Should().Be("trunk group sample updated with 2 trunks");
        updated.CapacityLimit.Should().Be(500);
    }

    [Fact]
    public async Task TestDeleteVoiceInTrunkGroup()
    {
        var id = "b2319703-ce6c-480d-bb53-614e7abcfc96";
        StubDelete("voice_in_trunk_groups/" + id);

        await Client.VoiceInTrunkGroups().DeleteAsync(id);
    }
}
