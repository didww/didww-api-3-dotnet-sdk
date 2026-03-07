using Didww.Api3.Http;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class AvailableDidTest : BaseTest
{
    [Fact]
    public async Task TestListAvailableDids()
    {
        StubGet("available_dids", "available_dids/index.json");

        var response = await Client.AvailableDids().ListAsync();
        var dids = response.Data;

        dids.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestFindAvailableDid()
    {
        StubGet("available_dids/0b76223b-9625-412f-b0f3-330551473e7e", "available_dids/show.json");

        var queryParams = new QueryParams().Include("did_group.stock_keeping_units");
        var response = await Client.AvailableDids().FindAsync("0b76223b-9625-412f-b0f3-330551473e7e", queryParams);
        var did = response.Data;

        did.Id.Should().Be("0b76223b-9625-412f-b0f3-330551473e7e");
        did.Number.Should().Be("16169886810");
        did.DidGroup.Should().NotBeNull();
    }

    [Fact]
    public async Task TestFindAvailableDidWithNanpaPrefix()
    {
        StubGet("available_dids/0e1c548e-c6b5-43b0-9c12-2e300178e820", "available_dids/show_with_nanpa_prefix.json");

        var queryParams = new QueryParams().Include("nanpa_prefix");
        var response = await Client.AvailableDids().FindAsync("0e1c548e-c6b5-43b0-9c12-2e300178e820", queryParams);
        var did = response.Data;

        did.Id.Should().Be("0e1c548e-c6b5-43b0-9c12-2e300178e820");
        did.Number.Should().Be("12012213879");
        did.NanpaPrefix.Should().NotBeNull();
        did.NanpaPrefix!.Npa.Should().Be("201");
        did.NanpaPrefix.Nxx.Should().Be("221");
    }
}
