using Didww.Api3.Http;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class CityTest : BaseTest
{
    [Fact]
    public async Task TestListCities()
    {
        StubGet("cities", "cities/index.json");

        var response = await Client.Cities().ListAsync();
        var cities = response.Data;

        cities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestFindCity()
    {
        StubGet("cities/368bf92f-c36e-473f-96fc-d53ed1b4028b", "cities/show.json");

        var queryParams = new QueryParams().Include("country", "region");
        var response = await Client.Cities().FindAsync("368bf92f-c36e-473f-96fc-d53ed1b4028b", queryParams);
        var city = response.Data;

        city.Id.Should().Be("368bf92f-c36e-473f-96fc-d53ed1b4028b");
        city.Name.Should().Be("New York");
        city.Country.Should().NotBeNull();
        city.Country!.Name.Should().Be("United States");
    }
}
