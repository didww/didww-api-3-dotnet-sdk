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

    [Fact]
    // https://doc.didww.com/api3/2022-05-10/callbacks-details.html#algorithm-implementation-details
    public void TestDocumentationExample()
    {
        var validator = new RequestValidator("szrdgh6547umt7tht7xbqhj6g9gdbyp7");
        var url = "https://mycompany.com/didww_callbacks?opaque=123";
        var payload = new Dictionary<string, string>
        {
            { "id", "bf2cee72-6caa-4ae2-917e-bea01945691e" },
            { "status", "completed" },
            { "type", "orders" }
        };

        validator.Validate(url, payload, "30f66e9d72eb5e193051fd02952f70d8e934b4ff").Should().BeTrue();
    }

    [Fact]
    public void TestNonHexSignature()
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "status", "completed" },
            { "type", "orders" }
        };

        validator.Validate("http://example.com/callbacks", payload, "not-hex").Should().BeFalse();
    }

    [Fact]
    public void TestOddLengthHexSignature()
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "status", "completed" },
            { "type", "orders" }
        };

        validator.Validate("http://example.com/callbacks", payload, "abc").Should().BeFalse();
    }

    [Theory]
    [InlineData("http://foo.com/bar", "4d1ce2be656d20d064183bec2ab98a2ff3981f73")] // NOSONAR
    [InlineData("http://foo.com:80/bar", "4d1ce2be656d20d064183bec2ab98a2ff3981f73")] // NOSONAR
    [InlineData("http://foo.com:443/bar", "904eaa65c0759afac0e4d8912de424e2dfb96ea1")] // NOSONAR
    [InlineData("http://foo.com:8182/bar", "eb8fcfb3d7ed4b4c2265d73cf93c31ba614384d1")] // NOSONAR
    [InlineData("http://foo.com/bar?baz=boo", "78b00717a86ce9df06abf45ff818aa94537e1729")] // NOSONAR
    [InlineData("http://user:pass@foo.com/bar", "88615a11a78c021c1da2e1e0bfb8cc165170afc5")] // NOSONAR
    [InlineData("http://foo.com/bar#test", "b1c4391fcdab7c0521bb5b9eb4f41f08529b8418")] // NOSONAR
    [InlineData("https://foo.com/bar", "f26a771c302319a7094accbe2989bad67fff2928")]
    [InlineData("https://foo.com:443/bar", "f26a771c302319a7094accbe2989bad67fff2928")]
    [InlineData("https://foo.com:80/bar", "bd45af5253b72f6383c6af7dc75250f12b73a4e1")]
    [InlineData("https://foo.com:8384/bar", "9c9fec4b7ebd6e1c461cb8e4ffe4f2987a19a5d3")]
    [InlineData("https://foo.com/bar?qwe=asd", "4a0e98ddf286acadd1d5be1b0ed85a4e541c3137")]
    [InlineData("https://qwe:asd@foo.com/bar", "7a8cd4a6c349910dfecaf9807e56a63787250bbd")] // NOSONAR
    [InlineData("https://foo.com/bar#baz", "5024919770ea5ca2e3ccc07cb940323d79819508")]
    [InlineData("http://[::1]/bar", "e0e9b83e4046d097f54b3ae64b08cbb4a539f601")]
    [InlineData("http://[::1]:80/bar", "e0e9b83e4046d097f54b3ae64b08cbb4a539f601")]
    [InlineData("http://[::1]:9090/bar", "ebec110ec5debd0e0fd086ff2f02e48ca665b543")]
    [InlineData("https://[::1]/bar", "f3cfe6f523fdf1d4eaadc310fcd3ed92e1e324b0")]
    [InlineData("http://foo.com/hello%20world", "eb64035b2e8f356ff1442898a39ec94d5c3e2fc8")]
    [InlineData("http://foo.com/foo%2Fbar", "db24428442b012fa0972a453ba1ba98e755bba10")]
    public void TestUrlNormalization(string url, string expectedSignature)
    {
        var validator = new RequestValidator("SOMEAPIKEY");
        var payload = new Dictionary<string, string>
        {
            { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
            { "status", "completed" },
            { "type", "orders" }
        };

        validator.Validate(url, payload, expectedSignature).Should().BeTrue();
    }
}
