using FluentAssertions;

namespace Didww.Api3.Tests;

public class ProofTypeTest : BaseTest
{
    [Fact]
    public async Task TestListProofTypes()
    {
        StubGet("proof_types", "proof_types/index.json");

        var response = await Client.ProofTypes().ListAsync();
        var types = response.Data;

        types.Should().NotBeEmpty();

        var first = types[0];
        first.Id.Should().Be("ab1fb565-ac55-4c73-bc55-64dc61e70169");
        first.Name.Should().Be("Utility Bill");
        first.EntityType.Should().Be("Address");
    }
}
