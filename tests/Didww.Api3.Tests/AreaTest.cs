using FluentAssertions;

namespace Didww.Api3.Tests;

public class AreaTest : BaseTest
{
    [Fact]
    public async Task TestListAreas()
    {
        StubGet("areas", "areas/index.json");

        var response = await Client.Areas().ListAsync();
        var areas = response.Data;

        areas.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestFindArea()
    {
        StubGet("areas/ab2adc18-7c94-42d9-bdde-b28dfc373a22", "areas/show.json");

        var response = await Client.Areas().FindAsync("ab2adc18-7c94-42d9-bdde-b28dfc373a22");
        var area = response.Data;

        area.Id.Should().Be("ab2adc18-7c94-42d9-bdde-b28dfc373a22");
        area.Name.Should().Be("Tuscany");
    }
}
