using FluentAssertions;

namespace Didww.Api3.Tests;

public class PopTest : BaseTest
{
    [Fact]
    public async Task TestListPops()
    {
        StubGet("pops", "pops/index.json");

        var response = await Client.Pops().ListAsync();
        var pops = response.Data;

        pops.Should().NotBeEmpty();

        var first = pops[0];
        first.Id.Should().Be("29dbdddf-3026-4e82-a2d6-5d8b3b2e0ad9");
        first.Name.Should().Be("New York, NY, USA");
    }
}
