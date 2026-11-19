using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using FluentAssertions;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Tests;

/// <summary>
/// An update that clears a relationship rewrites the payload; numbers and
/// strings must come out of that rewrite exactly as serialized.
/// </summary>
public class RepositoryUpdateTest : IDisposable
{
    private readonly FakeAuthServer _server = new();

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task TestClearedRelationshipKeepsDecimalScale()
    {
        _server.Api = (_, _) => Task.FromResult(FakeAuthServer.Fixture(200, "voice_out_trunks/update.json"));
        var client = _server.NewClient();
        var trunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.ThresholdAmount = 1.50m;
        trunk.EmergencyDids = null;

        await client.VoiceOutTrunks().UpdateAsync(trunk);

        var body = _server.ApiRequests.Should().ContainSingle().Subject.Body;
        body.Should().Contain("\"threshold_amount\":1.50");
        JObject.Parse(body).SelectToken("data.relationships.emergency_dids.data")
            .Should().BeOfType<JArray>().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task TestClearedRelationshipKeepsGenericConfigurationValues()
    {
        _server.Api = (_, _) => Task.FromResult(FakeAuthServer.Fixture(200, "sms_trunks/update_http_in.json"));
        var client = _server.NewClient();
        var trunk = SmsTrunk.Build("b4c3fa20-3d2b-4d7e-9a6f-0c9b2e0f6a11");
        trunk.Configuration = new GenericSmsTrunkConfiguration("future_transport", new JObject
        {
            ["ratio"] = 1.10m,
            ["since"] = "2026-10-22T10:00:00.5+02:00"
        });
        trunk.SmsTrunkGroup = null;

        await client.SmsTrunks().UpdateAsync(trunk);

        var body = _server.ApiRequests.Should().ContainSingle().Subject.Body;
        body.Should().Contain("\"ratio\":1.10");
        body.Should().Contain("\"since\":\"2026-10-22T10:00:00.5+02:00\"");
        JObject.Parse(body).SelectToken("data.relationships.sms_trunk_group")!.ToString(Newtonsoft.Json.Formatting.None)
            .Should().Be("{\"data\":null}");
    }
}
