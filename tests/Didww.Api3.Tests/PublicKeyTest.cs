using FluentAssertions;

namespace Didww.Api3.Tests;

public class PublicKeyTest : BaseTest
{
    [Fact]
    public async Task TestListPublicKeys()
    {
        StubGet("public_keys", "public_keys/index.json");

        var response = await Client.PublicKeys().ListAsync();
        var keys = response.Data;

        keys.Should().NotBeEmpty();

        var first = keys[0];
        first.Id.Should().Be("dcf2bfcb-a1d0-3b58-bbf0-3ec22a510ba8");
        first.Key.Should().StartWith("-----BEGIN PUBLIC KEY-----");
    }
}
