using Didww.Api3.Exception;
using Didww.Api3.Resource;
using FluentAssertions;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Didww.Api3.Tests;

public class ErrorHandlingTest : BaseTest
{
    [Fact]
    public async Task TestApiErrorResponse()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/countries").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody("""
                {
                    "errors": [
                        {
                            "title": "must be present",
                            "detail": "name - must be present",
                            "code": "100",
                            "source": {"pointer": "/data/attributes/name"},
                            "status": "422"
                        }
                    ]
                }
                """)
        );

        var act = () => Client.Countries().ListAsync();
        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(422);
        ex.Which.Errors.Should().HaveCount(1);
        ex.Which.Errors[0].Title.Should().Be("must be present");
        ex.Which.Errors[0].Detail.Should().Be("name - must be present");
        ex.Which.Errors[0].Code.Should().Be("100");
    }

    [Fact]
    public async Task TestApiErrorWithoutErrors()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/countries").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(500)
                .WithBody("Internal Server Error")
        );

        var act = () => Client.Countries().ListAsync();
        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(500);
    }

    [Fact]
    public async Task TestNotFoundError()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/countries/nonexistent").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(404)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody("""
                {
                    "errors": [
                        {
                            "title": "Record not found",
                            "status": "404"
                        }
                    ]
                }
                """)
        );

        var act = () => Client.Countries().FindAsync("nonexistent");
        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(404);
        // Verify message falls back to Title when Detail is missing
        ex.Which.Message.Should().Contain("Record not found");
        ex.Which.Errors.Should().HaveCount(1);
        ex.Which.Errors[0].Title.Should().Be("Record not found");
        ex.Which.Errors[0].Detail.Should().BeNull();
    }

    [Fact]
    public async Task TestErrorMessageUsesDetailWhenBothPresent()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/countries").UsingGet()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody("""
                {
                    "errors": [
                        {
                            "title": "must be present",
                            "detail": "name - must be present",
                            "status": "422"
                        }
                    ]
                }
                """)
        );

        var act = () => Client.Countries().ListAsync();
        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.Message.Should().Contain("name - must be present");
        ex.Which.Message.Should().NotContain("must be present; ");
    }
}
