using Didww.Api3.Auth;
using FluentAssertions;
using Newtonsoft.Json;

namespace Didww.Api3.Tests;

public class DidwwTokenTest
{
    private static DidwwToken TestToken() => new()
    {
        AccessToken = "secret-access-token",
        TokenType = "Bearer",
        ExpiresIn = 900,
        Audience = new[] { "api.didww.com" },
        Scope = "full",
        ApiVersion = "2026-10-22",
        MintedAt = new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero)
    };

    [Fact]
    public void TestToStringHidesAccessToken()
    {
        var text = TestToken().ToString();

        text.Should().NotContain("secret-access-token");
        text.Should().Contain("[FILTERED]");
        text.Should().Contain("api.didww.com");
        text.Should().Contain("2026-10-22");
        new List<DidwwToken> { TestToken() }.Aggregate("", (s, t) => s + t).Should().NotContain("secret-access-token");
    }

    [Fact]
    public void TestNewtonsoftRoundTrip()
    {
        var json = JsonConvert.SerializeObject(TestToken());
        json.Should().Contain("\"access_token\":\"secret-access-token\"");
        json.Should().Contain("\"aud\":[\"api.didww.com\"]");

        var decoded = JsonConvert.DeserializeObject<DidwwToken>(json)!;

        decoded.Should().BeEquivalentTo(TestToken());
    }

    [Fact]
    public void TestSystemTextJsonRoundTrip()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(TestToken());
        json.Should().Contain("\"access_token\":\"secret-access-token\"");
        json.Should().Contain("\"api_version\":\"2026-10-22\"");

        var decoded = System.Text.Json.JsonSerializer.Deserialize<DidwwToken>(json)!;

        decoded.Should().BeEquivalentTo(TestToken());
    }

    [Fact]
    public void TestTokenFromFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "fixtures", "token", "create.json");

        var token = JsonConvert.DeserializeObject<DidwwToken>(File.ReadAllText(path))!;

        token.AccessToken.Should().Be("test-access-token");
        token.TokenType.Should().Be("Bearer");
        token.ExpiresIn.Should().Be(900);
        token.Audience.Should().Equal("api.didww.com");
        token.Scope.Should().Be("full");
        token.ApiVersion.Should().Be("2026-10-22");
    }
}
