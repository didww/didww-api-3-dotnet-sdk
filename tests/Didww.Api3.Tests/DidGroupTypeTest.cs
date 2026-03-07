using FluentAssertions;

namespace Didww.Api3.Tests;

public class DidGroupTypeTest : BaseTest
{
    [Fact]
    public async Task TestListDidGroupTypes()
    {
        StubGet("did_group_types", "did_group_types/index.json");

        var response = await Client.DidGroupTypes().ListAsync();
        var types = response.Data;

        types.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestFindDidGroupType()
    {
        StubGet("did_group_types/d6530a8c-924c-469a-98c0-9525602e6192", "did_group_types/show.json");

        var response = await Client.DidGroupTypes().FindAsync("d6530a8c-924c-469a-98c0-9525602e6192");
        var groupType = response.Data;

        groupType.Id.Should().Be("d6530a8c-924c-469a-98c0-9525602e6192");
        groupType.Name.Should().Be("Global");
    }
}
