using Didww.Api3.Http;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class NanpaPrefixTest : BaseTest
{
    [Fact]
    public async Task TestListNanpaPrefixes()
    {
        StubGet("nanpa_prefixes", "nanpa_prefixes/index.json");

        var response = await Client.NanpaPrefixes().ListAsync();
        var prefixes = response.Data;

        prefixes.Should().NotBeEmpty();

        var first = prefixes[0];
        first.Id.Should().Be("54943e12-88e9-4df9-be54-a72926c251dd");
        first.Npa.Should().Be("864");
        first.Nxx.Should().Be("200");
    }

    [Fact]
    public async Task TestFindNanpaPrefix()
    {
        StubGet("nanpa_prefixes/6c16d51d-d376-4395-91c4-012321317e48", "nanpa_prefixes/show.json");

        var queryParams = new QueryParams().Include("country");
        var response = await Client.NanpaPrefixes().FindAsync("6c16d51d-d376-4395-91c4-012321317e48", queryParams);
        var prefix = response.Data;

        prefix.Id.Should().Be("6c16d51d-d376-4395-91c4-012321317e48");
        prefix.Npa.Should().Be("864");
        prefix.Nxx.Should().Be("920");
        prefix.Country.Should().NotBeNull();
        prefix.Country!.Name.Should().Be("United States");
    }
}
