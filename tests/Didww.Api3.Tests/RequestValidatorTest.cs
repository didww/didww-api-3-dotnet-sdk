using Didww.Api3.Callback;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class RequestValidatorTest
{
    [Fact]
    public void TestSandbox()
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var url = "http://example.com/callback.php?id=7ae7c48f-d48a-499f-9dc1-c9217014b457&reject_reason=&status=approved&type=address_verifications";
        var payload = new Dictionary<string, string>
        {
            { "status", "approved" },
            { "id", "7ae7c48f-d48a-499f-9dc1-c9217014b457" },
            { "type", "address_verifications" },
            { "reject_reason", "" }
        };

        validator.Validate(url, payload, "18050028b6b22d0ed516706fba1c1af8d6a8f9d5").Should().BeTrue();
    }

    [Fact]
    public void TestValidRequest()
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "type", "orders" }
        };

        validator.Validate("http://example.com/callbacks", payload, "fe99e416c3547f2f59002403ec856ea386d05b2f").Should().BeTrue();
    }

    [Fact]
    public void TestValidRequestWithQueryAndFragment()
    {
        var validator = new RequestValidator("OTHERAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "type", "orders" }
        };

        validator.Validate("http://example.com/callbacks?foo=bar#baz", payload, "32754ba93ac1207e540c0cf90371e7786b3b1cde").Should().BeTrue();
    }

    [Fact]
    public void TestEmptySignatureRequest()
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "type", "orders" }
        };

        validator.Validate("http://example.com/callbacks", payload, "").Should().BeFalse();
    }

    [Fact]
    public void TestInvalidSignatureRequest()
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "type", "orders" }
        };

        validator.Validate("http://example.com/callbacks", payload, "fbdb1d1b18aa6c08324b7d64b71fb76370690e1d").Should().BeFalse();
    }

    [Fact]
    public void TestNullSignature()
    {
        var validator = new RequestValidator("key");
        var payload = new Dictionary<string, string> { { "a", "b" } };
        validator.Validate("http://example.com", payload, null).Should().BeFalse();
    }

    [Fact]
    public void TestHttpsDefaultPort()
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "status", "completed" },
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "type", "orders" }
        };

        // HTTPS URL without explicit port should normalize to :443
        validator.Validate("https://example.com/callbacks", payload, "57ba6c3c14ea4bfa9bebd079869cafb27dcba1b6").Should().BeTrue();
    }
}
