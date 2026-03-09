using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class VoiceOutTrunkTest : BaseTest
{
    [Fact]
    public async Task TestListVoiceOutTrunks()
    {
        StubGet("voice_out_trunks", "voice_out_trunks/index.json");

        var response = await Client.VoiceOutTrunks().ListAsync();
        var trunks = response.Data;

        trunks.Should().NotBeEmpty();

        var first = trunks[0];
        first.Id.Should().Be("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        first.Name.Should().Be("test");
        first.Status.Should().Be(VoiceOutTrunkStatus.Blocked);
        first.OnCliMismatchAction.Should().Be(OnCliMismatchAction.ReplaceCli);
        first.CapacityLimit.Should().Be(123);
        first.AllowAnyDidAsCli.Should().BeFalse();
        first.MediaEncryptionMode.Should().Be(MediaEncryptionMode.SrtpSdes);
        first.DefaultDstAction.Should().Be(DefaultDstAction.RejectAll);
        first.ForceSymmetricRtp.Should().BeTrue();
        first.RtpPing.Should().BeTrue();
        first.ThresholdReached.Should().BeFalse();
        first.ThresholdAmount.Should().Be(200.0m);
        first.Username.Should().Be("dpjgwbbac9");
        first.Password.Should().Be("z0hshvbcy7");
        first.DstPrefixes.Should().ContainSingle().Which.Should().Be("370");
    }

    [Fact]
    public async Task TestFindVoiceOutTrunk()
    {
        StubGet("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864", "voice_out_trunks/show.json");

        var response = await Client.VoiceOutTrunks().FindAsync("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        var trunk = response.Data;

        trunk.Id.Should().Be("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.Name.Should().Be("test");
        trunk.Dids.Should().HaveCount(2);
        trunk.DefaultDid.Should().NotBeNull();
        trunk.DefaultDid!.Number.Should().Be("37061498222");
    }

    [Fact]
    public async Task TestCreateVoiceOutTrunk()
    {
        StubPost("voice_out_trunks",
            "voice_out_trunks/create_request.json", "voice_out_trunks/create.json");

        var did = Did.Build("7a028c32-e6b6-4c86-bf01-90f901b37012");
        var trunk = new VoiceOutTrunk
        {
            Name = "java-test",
            AllowedSipIps = new List<string> { "0.0.0.0/0" },
            OnCliMismatchAction = OnCliMismatchAction.ReplaceCli,
            DefaultDid = did,
            Dids = new List<Did> { did }
        };

        var response = await Client.VoiceOutTrunks().CreateAsync(trunk);
        var created = response.Data;

        created.Id.Should().Be("b60201c1-21f0-4d9a-aafa-0e6d1e12f22e");
        created.Name.Should().Be("java-test");
        created.Status.Should().Be(VoiceOutTrunkStatus.Active);
        created.Username.Should().NotBeNullOrEmpty();
        created.Password.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkSendsOnlyDirtyFields()
    {
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_request.json", "voice_out_trunks/update.json");

        var trunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.Name = "test";
        trunk.CapacityLimit = 123;
        trunk.OnCliMismatchAction = OnCliMismatchAction.ReplaceCli;
        trunk.DefaultDstAction = DefaultDstAction.RejectAll;
        trunk.DstPrefixes = new List<string> { "370" };
        trunk.ForceSymmetricRtp = true;
        trunk.RtpPing = true;
        trunk.AllowedSipIps = new List<string> { "10.11.12.13/32" };

        var response = await Client.VoiceOutTrunks().UpdateAsync(trunk);
        var updated = response.Data;

        updated.Id.Should().Be("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        updated.Name.Should().Be("test");
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkFromLoadedResource()
    {
        StubGet("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864", "voice_out_trunks/show.json");
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_from_loaded_request.json", "voice_out_trunks/update.json");

        var trunk = (await Client.VoiceOutTrunks().FindAsync("425ce763-a3a9-49b4-af5b-ada1a65c8864")).Data;
        trunk.CallbackUrl = "https://example.com/callback";
        trunk.AllowAnyDidAsCli = true;
        trunk.ThresholdAmount = 500.0m;

        await Client.VoiceOutTrunks().UpdateAsync(trunk);
    }

    [Fact]
    public async Task TestDeleteVoiceOutTrunk()
    {
        var id = "425ce763-a3a9-49b4-af5b-ada1a65c8864";
        StubDelete("voice_out_trunks/" + id);

        await Client.VoiceOutTrunks().DeleteAsync(id);
    }
}
