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
    }
}

public class DidwwClientExceptionTest
{
    [Fact]
    public void TestConstructorWithMessage()
    {
        var ex = new DidwwClientException("something failed");
        ex.Message.Should().Be("something failed");
        ex.InnerException.Should().BeNull();
    }

    [Fact]
    public void TestConstructorWithInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new DidwwClientException("outer", inner);
        ex.Message.Should().Be("outer");
        ex.InnerException.Should().Be(inner);
    }
}

public class DidwwApiExceptionTest
{
    [Fact]
    public void TestConstructorWithMessageOnly()
    {
        var ex = new DidwwApiException(503, "Service Unavailable");
        ex.HttpStatus.Should().Be(503);
        ex.Message.Should().Be("Service Unavailable");
        ex.Errors.Should().BeEmpty();
    }

    [Fact]
    public void TestApiErrorToString()
    {
        var error = new ApiError
        {
            Title = "validation error",
            Detail = "name is required"
        };
        error.ToString().Should().Contain("validation error");
        error.ToString().Should().Contain("name is required");
    }

    [Fact]
    public void TestMultipleErrors()
    {
        var errors = new List<ApiError>
        {
            new() { Title = "err1", Detail = "detail1" },
            new() { Title = "err2", Detail = "detail2" }
        };
        var ex = new DidwwApiException(422, errors);
        ex.Message.Should().Contain("detail1");
        ex.Message.Should().Contain("detail2");
        ex.Errors.Should().HaveCount(2);
    }
}
