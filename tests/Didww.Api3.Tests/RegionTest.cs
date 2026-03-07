using Didww.Api3.Http;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class RegionTest : BaseTest
{
    [Fact]
    public async Task TestListRegions()
    {
        StubGet("regions", "regions/index.json");

        var response = await Client.Regions().ListAsync();
        var regions = response.Data;

        regions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestFindRegion()
    {
        StubGet("regions/c11b1f34-16cf-4ba6-8497-f305b53d5b01", "regions/show.json");

        var queryParams = new QueryParams().Include("country");
        var response = await Client.Regions().FindAsync("c11b1f34-16cf-4ba6-8497-f305b53d5b01", queryParams);
        var region = response.Data;

        region.Id.Should().Be("c11b1f34-16cf-4ba6-8497-f305b53d5b01");
        region.Name.Should().Be("California");
        region.Country.Should().NotBeNull();
        region.Country!.Name.Should().Be("United States");
    }
}
