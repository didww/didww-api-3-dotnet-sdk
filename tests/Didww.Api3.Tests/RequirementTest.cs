using FluentAssertions;

namespace Didww.Api3.Tests;

public class RequirementTest : BaseTest
{
    [Fact]
    public async Task TestListRequirements()
    {
        StubGet("requirements", "requirements/index.json");

        var response = await Client.Requirements().ListAsync();
        var requirements = response.Data;

        requirements.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestFindRequirement()
    {
        StubGet("requirements/25d12afe-1ec6-4fe3-9621-b250dd1fb959", "requirements/show.json");

        var response = await Client.Requirements().FindAsync("25d12afe-1ec6-4fe3-9621-b250dd1fb959");
        var requirement = response.Data;

        requirement.Id.Should().Be("25d12afe-1ec6-4fe3-9621-b250dd1fb959");
    }
}
