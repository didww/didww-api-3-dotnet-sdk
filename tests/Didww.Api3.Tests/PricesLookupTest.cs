using System.Globalization;
using System.Net;
using System.Web;
using Didww.Api3.Exception;
using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class PricesLookupTest
{
    private const string UnauthorizedBody =
        """{"errors":[{"title":"Unauthorized","code":"401","status":"401"}]}""";

    private static Dictionary<string, string> Query(Uri uri)
    {
        var parsed = HttpUtility.ParseQueryString(uri.Query);
        return parsed.AllKeys.ToDictionary(key => key!, key => parsed[key]!);
    }

    private static string Text(decimal? value) => value!.Value.ToString(CultureInfo.InvariantCulture);

    private static Task Lookup(DidwwClient client, string service, Dictionary<string, string?>? filters) => service switch
    {
        "VoiceIn" => client.VoiceInPrices().LookupAsync(filters!),
        "VoiceOut" => client.VoiceOutPrices().LookupAsync(filters!),
        "SmsOutA2P" => client.SmsOutA2PPrices().LookupAsync(filters!),
        "SmsOutP2P" => client.SmsOutP2PPrices().LookupAsync(filters!),
        "SmsOutOtp" => client.SmsOutOtpPrices().LookupAsync(filters!),
        "SmsIn" => client.SmsInPrices().LookupAsync(filters!),
        _ => throw new ArgumentOutOfRangeException(nameof(service))
    };

    [Fact]
    public async Task TestVoiceInPricesLookup()
    {
        var fixture = FakeAuthServer.LoadFixture("prices/voice_in/lookup/200.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, fixture));
        var client = server.NewClient();

        var result = await client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var request = server.Requests.Should().ContainSingle().Subject;
        request.Method.Should().Be("GET");
        request.Uri.GetLeftPart(UriPartial.Path).Should().Be("https://vin.example.test/v3/lookup/prices");
        Query(request.Uri).Should().Equal(new Dictionary<string, string> { ["filter[number]"] = "18005550123" });
        request.Headers["Accept"].Should().Be("application/vnd.api+json");
        request.Authorization.Should().Be("Bearer token-1");
        request.Headers["User-Agent"].Should().Be(DidwwClient.SdkUserAgent);
        request.Headers.Should().NotContainKey("Api-Key");
        server.MintAudiences().Should().Equal("api-vin.didww.com");

        result.TotalRecords.Should().Be(2);
        result.Rows.Should().HaveCount(2);
        var priced = result.Rows[0];
        var rejected = result.Rows[1];
        priced.Id.Should().Be("df73d9e4-46ce-4af0-817e-f090dc8144f1");
        priced.Type.Should().Be("prices");
        priced.Destination.Should().Be("1800");
        priced.Country.Should().Be("United States");
        priced.SrcCountryName.Should().BeNull();
        priced.SourceType.Should().Be("cellular");
        Text(priced.Rate).Should().Be("0.025");
        Text(priced.ConnectionFee).Should().Be("0.0");
        priced.InitialInterval.Should().Be(60);
        priced.NextInterval.Should().Be(60);
        priced.RejectCalls.Should().BeFalse();
        rejected.Id.Should().Be("cb1335e1-6cfd-4921-8724-153d9ed67ac3");
        rejected.SrcCountryName.Should().Be("United Kingdom");
        rejected.Rate.Should().BeNull();
        rejected.ConnectionFee.Should().BeNull();
        rejected.RejectCalls.Should().BeTrue();
    }

    [Fact]
    public async Task TestVoiceInPricesLookupWithSourceCountry()
    {
        var fixture = FakeAuthServer.LoadFixture("prices/voice_in/lookup/200.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, fixture));
        var client = server.NewClient();

        await client.VoiceInPrices().LookupAsync(new Dictionary<string, string?>
        {
            ["src_country_iso"] = "GB",
            ["number"] = "448001234567"
        });

        var request = server.Requests.Should().ContainSingle().Subject;
        request.Uri.Query.Should().Be("?filter[number]=448001234567&filter[src_country_iso]=GB", "filters are sorted by name");
        Query(request.Uri).Should().Equal(new Dictionary<string, string>
        {
            ["filter[number]"] = "448001234567",
            ["filter[src_country_iso]"] = "GB"
        });
    }

    [Fact]
    public async Task TestPricesLookupEscapesFilterValues()
    {
        var fixture = FakeAuthServer.LoadFixture("prices/voice_in/lookup/200.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, fixture));
        var client = server.NewClient();

        await client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "+1 800&x=1" });

        Query(server.Requests.Single().Uri).Should().Equal(new Dictionary<string, string>
        {
            ["filter[number]"] = "+1 800&x=1"
        });
    }

    [Fact]
    public async Task TestVoiceOutPricesLookup()
    {
        var fixture = FakeAuthServer.LoadFixture("prices/voice_out/lookup/200.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, fixture));
        var client = server.NewClient();

        var result = await client.VoiceOutPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "493012345678" });

        var request = server.Requests.Should().ContainSingle().Subject;
        request.Uri.GetLeftPart(UriPartial.Path).Should().Be("https://vout.example.test/v3/lookup/prices");
        Query(request.Uri).Should().Equal(new Dictionary<string, string> { ["filter[number]"] = "493012345678" });
        server.MintAudiences().Should().Equal("api-vout.didww.com");

        result.TotalRecords.Should().Be(2);
        result.Rows.Should().HaveCount(2);
        var scheduled = result.Rows[0];
        var plain = result.Rows[1];
        scheduled.Id.Should().Be("7b82cac2-c42a-4fa9-8da4-ccba2843068f");
        scheduled.Category.Should().Be(VoiceOutPriceCategory.International);
        scheduled.Country.Should().Be("Germany");
        scheduled.Network.Should().Be("Fixed");
        scheduled.Prefix.Should().Be("4930");
        Text(scheduled.Rate).Should().Be("0.0045");
        Text(scheduled.ConnectionFee).Should().Be("0.0");
        scheduled.InitialIncrement.Should().Be(1);
        scheduled.RecurringIncrement.Should().Be(1);
        Text(scheduled.NextRate).Should().Be("0.005");
        Text(scheduled.NextConnectionFee).Should().Be("0.0");
        scheduled.NextInitialIncrement.Should().Be(30);
        scheduled.NextRecurringIncrement.Should().Be(6);
        scheduled.EffectiveAt.Should().Be(new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.Zero));
        scheduled.Comment.Should().Be("Increase and Billing increment change");
        scheduled.RejectCalls.Should().BeFalse();
        plain.Category.Should().Be(VoiceOutPriceCategory.OriginBased);
        plain.NextRate.Should().BeNull();
        plain.NextConnectionFee.Should().BeNull();
        plain.NextInitialIncrement.Should().BeNull();
        plain.NextRecurringIncrement.Should().BeNull();
        plain.EffectiveAt.Should().BeNull();
        plain.Comment.Should().BeNull();
    }

    [Theory]
    [InlineData("SmsOutA2P", "/v3/sms_out/a2p/lookup/prices")]
    [InlineData("SmsOutP2P", "/v3/sms_out/p2p/lookup/prices")]
    [InlineData("SmsOutOtp", "/v3/sms_out/otp/lookup/prices")]
    [InlineData("SmsIn", "/v3/sms_in/lookup/prices")]
    public async Task TestSmsPricesLookup(string service, string path)
    {
        var fixture = FakeAuthServer.LoadFixture(service == "SmsIn" ? "prices/sms_in/lookup/200.json" : "prices/sms_out/lookup/200.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, fixture));
        var client = server.NewClient();

        await Lookup(client, service, new Dictionary<string, string?> { ["dst_number"] = "4915123456789" });
        await Lookup(client, service, new Dictionary<string, string?> { ["dst_country_iso"] = "FR" });
        await Lookup(client, service, new Dictionary<string, string?>
        {
            ["dst_number"] = "4915123456789",
            ["dst_country_iso"] = "DE"
        });

        var requests = server.Requests;
        requests.Should().HaveCount(3);
        foreach (var request in requests)
        {
            request.Uri.GetLeftPart(UriPartial.Path).Should().Be("https://sms.example.test" + path);
            request.Authorization.Should().Be("Bearer token-1");
        }
        Query(requests[0].Uri).Should().Equal(new Dictionary<string, string> { ["filter[dst_number]"] = "4915123456789" });
        Query(requests[1].Uri).Should().Equal(new Dictionary<string, string> { ["filter[dst_country_iso]"] = "FR" });
        Query(requests[2].Uri).Should().Equal(new Dictionary<string, string>
        {
            ["filter[dst_country_iso]"] = "DE",
            ["filter[dst_number]"] = "4915123456789"
        });
        server.MintAudiences().Should().Equal("api-sms.didww.com");
    }

    [Fact]
    public async Task TestSmsOutPricesLookupRows()
    {
        var fixture = FakeAuthServer.LoadFixture("prices/sms_out/lookup/200.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, fixture));
        var client = server.NewClient();

        var result = await client.SmsOutA2PPrices().LookupAsync(new Dictionary<string, string?> { ["dst_number"] = "4915123456789" });

        result.TotalRecords.Should().Be(2);
        result.Rows.Should().HaveCount(2);
        var priced = result.Rows[0];
        var rejected = result.Rows[1];
        priced.Id.Should().Be("d8e4d372-42a6-4bb6-80ec-6c29c49aa9fd");
        priced.SourcePrefix.Should().BeNull();
        priced.DestinationPrefix.Should().Be("49151");
        priced.Country.Should().Be("Germany");
        priced.Mcc.Should().Be(262);
        priced.MccName.Should().Be("Germany");
        priced.Mnc.Should().Be(1);
        priced.MncName.Should().Be("Telekom Deutschland");
        priced.RejectSms.Should().BeFalse();
        Text(priced.Price).Should().Be("0.075000", "the decimal keeps the scale of the answer");
        rejected.RejectSms.Should().BeTrue();
        rejected.Price.Should().BeNull();
    }

    [Fact]
    public async Task TestSmsInPricesLookupRows()
    {
        var fixture = FakeAuthServer.LoadFixture("prices/sms_in/lookup/200.json");
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, fixture));
        var client = server.NewClient();

        var result = await client.SmsInPrices().LookupAsync(new Dictionary<string, string?> { ["dst_number"] = "12025550123" });

        result.TotalRecords.Should().Be(1);
        var price = result.Rows.Should().ContainSingle().Subject;
        price.Id.Should().Be("e93f925e-25ee-4060-bd50-a790b4945d3f");
        price.SourcePrefix.Should().BeNull();
        price.SourceNumberLengthMin.Should().Be(1);
        price.SourceNumberLengthMax.Should().Be(15);
        price.DestinationPrefix.Should().Be("1");
        price.DestinationNumberLengthMin.Should().Be(11);
        price.DestinationNumberLengthMax.Should().Be(11);
        price.Country.Should().Be("United States");
        price.Price.ToString(CultureInfo.InvariantCulture).Should().Be("0.005000");
    }

    public static IEnumerable<object?[]> FilterValidationCases() => new List<object?[]>
    {
        new object?[] { "VoiceIn", new Dictionary<string, string?> { ["src_country_iso"] = "GB" },
            "Filter number is required on lookup/prices" },
        new object?[] { "VoiceIn", new Dictionary<string, string?> { ["number"] = "" },
            "Filter number is required on lookup/prices" },
        new object?[] { "VoiceIn", new Dictionary<string, string?> { ["number"] = null },
            "Filter number is required on lookup/prices" },
        new object?[] { "VoiceIn", new Dictionary<string, string?> { ["number"] = "18005550123", ["dst_country_iso"] = "US" },
            "Filter dst_country_iso is not allowed on lookup/prices (allowed: number, src_country_iso)" },
        new object?[] { "VoiceOut", new Dictionary<string, string?> { ["number"] = "493012345678", ["src_country_iso"] = "DE" },
            "Filter src_country_iso is not allowed on lookup/prices (allowed: number)" },
        new object?[] { "VoiceOut", new Dictionary<string, string?> { ["number"] = "493012345678", ["b"] = "1", ["a"] = "1" },
            "Filter a, b is not allowed on lookup/prices (allowed: number)" },
        new object?[] { "SmsOutA2P", new Dictionary<string, string?>(),
            "One of filters dst_number, dst_country_iso is required on sms_out/a2p/lookup/prices" },
        new object?[] { "SmsOutP2P", null,
            "One of filters dst_number, dst_country_iso is required on sms_out/p2p/lookup/prices" },
        new object?[] { "SmsOutOtp", new Dictionary<string, string?> { ["dst_number"] = " " },
            "One of filters dst_number, dst_country_iso is required on sms_out/otp/lookup/prices" },
        new object?[] { "SmsIn", new Dictionary<string, string?> { ["number"] = "12025550123" },
            "Filter number is not allowed on sms_in/lookup/prices (allowed: dst_number, dst_country_iso)" }
    };

    [Theory]
    [MemberData(nameof(FilterValidationCases))]
    public async Task TestPricesLookupFilterValidation(string service, Dictionary<string, string?>? filters, string message)
    {
        using var server = new PricesServer((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var client = server.NewClient();

        var act = () => Lookup(client, service, filters);

        (await act.Should().ThrowAsync<ArgumentException>()).Which.Message.Should().StartWith(message);
        server.Requests.Should().BeEmpty();
        server.Auth.Mints.Should().Be(0);
    }

    public static IEnumerable<object?[]> LookupErrorCases() => new List<object?[]>
    {
        new object?[] { 400, null, "prices/errors/filter_not_allowed.json", null,
            DidwwPricesErrorKind.BadRequest, "102", null, "Prices service: src_country_iso is not allowed." },
        new object?[] { 403, """{"errors":[{"title":"Insufficient scope","code":"403","status":"403"}]}""", null, null,
            DidwwPricesErrorKind.AccessDenied, "403", null, "Prices service: Insufficient scope" },
        new object?[] { 404, null, "prices/errors/price_not_found.json", null,
            DidwwPricesErrorKind.PriceNotFound, "price_not_found", null,
            "Prices service: No effective price covers this number in the assigned plan." },
        new object?[] { 404, null, "prices/errors/price_plan_not_assigned.json", null,
            DidwwPricesErrorKind.PricePlanNotAssigned, "price_plan_not_assigned", null,
            "Prices service: No price plan is assigned to this account." },
        new object?[] { 404, """{"errors":[{"title":"Not found","code":"not_found"}]}""", null, null,
            DidwwPricesErrorKind.PricePlanNotAssigned, "not_found", null, "Prices service: Not found" },
        new object?[] { 404, "", null, null,
            DidwwPricesErrorKind.PricePlanNotAssigned, "price_plan_not_assigned", null,
            "Prices service responded with status 404" },
        new object?[] { 406, """{"errors":[{"title":"Not acceptable","code":"406","status":"406"}]}""", null, null,
            DidwwPricesErrorKind.NotAcceptable, "406", null, "Prices service: Not acceptable" },
        new object?[] { 429, """{"errors":[{"title":"Too many requests","code":"too_many_requests","status":"429"}]}""", null, "7",
            DidwwPricesErrorKind.TooManyRequests, "too_many_requests", 7, "Prices service: Too many requests" },
        new object?[] { 503, null, "prices/errors/serving_failure.json", "1",
            DidwwPricesErrorKind.ServiceUnavailable, "serving_failure", 1, "Prices service: The price file is not available yet." },
        new object?[] { 500, "", null, null,
            DidwwPricesErrorKind.UnexpectedStatus, null, null, "Prices service responded with status 500" },
        new object?[] { 500, "<html>oops</html>", null, null,
            DidwwPricesErrorKind.UnexpectedStatus, null, null, "Prices service responded with status 500" },
        new object?[] { 500, """{"errors":["not an object",{"title":"Kept"}]}""", null, null,
            DidwwPricesErrorKind.UnexpectedStatus, null, null, "Prices service: Kept" },
        new object?[] { 304, "", null, null,
            DidwwPricesErrorKind.UnexpectedStatus, null, null, "Prices service responded with status 304" }
    };

    [Theory]
    [MemberData(nameof(LookupErrorCases))]
    public async Task TestPricesLookupErrors(int status, string? body, string? fixture, string? retryAfter,
        DidwwPricesErrorKind kind, string? code, int? retryAfterSeconds, string message)
    {
        var content = fixture != null ? FakeAuthServer.LoadFixture(fixture) : body!;
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(status, content, retryAfter));
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(kind);
        e.StatusCode.Should().Be(status);
        e.Code.Should().Be(code);
        e.RetryAfter.Should().Be(retryAfterSeconds.HasValue ? TimeSpan.FromSeconds(retryAfterSeconds.Value) : (TimeSpan?)null);
        e.Message.Should().Be(message);
        e.Should().BeAssignableTo<DidwwException>();
        server.Requests.Should().ContainSingle("lookups are never retried");
        server.Auth.Mints.Should().Be(1);
    }

    [Fact]
    public async Task TestPricesLookupDoesNotFollowRedirects()
    {
        using var server = new PricesServer((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Found);
            response.Headers.Location = new Uri("https://vin.example.test/v3/elsewhere");
            return response;
        });
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.UnexpectedStatus);
        e.StatusCode.Should().Be(302);
        server.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task TestPricesLookupRetriesRefusedTokenOnce()
    {
        var fixture = FakeAuthServer.LoadFixture("prices/voice_in/lookup/200.json");
        using var server = new PricesServer((n, _) => n == 1
            ? PricesServer.JsonApi(401, UnauthorizedBody)
            : PricesServer.JsonApi(200, fixture));
        var client = server.NewClient();

        var result = await client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        result.TotalRecords.Should().Be(2);
        server.Requests.Select(r => r.Authorization).Should().Equal("Bearer token-1", "Bearer token-2");
        server.MintAudiences().Should().Equal("api-vin.didww.com", "api-vin.didww.com");
    }

    [Fact]
    public async Task TestPricesLookupNotAuthorizedAfterRetry()
    {
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(401, UnauthorizedBody));
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.NotAuthorized);
        e.StatusCode.Should().Be(401);
        e.Code.Should().Be("401");
        server.Requests.Should().HaveCount(2);
        server.Auth.Mints.Should().Be(2);
    }

    [Fact]
    public async Task TestPricesLookupConnectionError()
    {
        var failure = new HttpRequestException("connection refused");
        using var server = new PricesServer((_, _) => throw failure);
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ConnectionError);
        e.StatusCode.Should().BeNull();
        e.InnerException.Should().BeSameAs(failure);
        e.Message.Should().Be("Prices service: connection refused");
    }

    [Fact]
    public async Task TestPricesLookupTimeoutIsConnectionError()
    {
        using var server = new PricesServer((_, _) => throw new TaskCanceledException("timed out"));
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        var e = (await act.Should().ThrowAsync<DidwwPricesException>()).Which;
        e.Kind.Should().Be(DidwwPricesErrorKind.ConnectionError);
        e.InnerException.Should().BeAssignableTo<OperationCanceledException>();
    }

    [Fact]
    public async Task TestPricesLookupCancelledByCaller()
    {
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, "{}"));
        var client = server.NewClient();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => client.VoiceInPrices()
            .LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" }, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        server.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task TestPricesLookupReturnsTokenErrorAsIs()
    {
        using var server = new PricesServer((_, _) => new HttpResponseMessage(HttpStatusCode.OK));
        server.Auth.Mint = (_, _, _) =>
            Task.FromResult(FakeAuthServer.Json(401, FakeAuthServer.LoadFixture("token/invalid_client.json")));
        var client = server.NewClient();

        var act = () => client.VoiceInPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "18005550123" });

        (await act.Should().ThrowAsync<DidwwAuthException>()).Which.Kind.Should().Be(DidwwAuthErrorKind.InvalidClient);
        server.Requests.Should().BeEmpty();
    }

    [Fact]
    public void TestPricesExceptionMessage()
    {
        new DidwwPricesException(DidwwPricesErrorKind.BadRequest, 400, errors: new List<ApiError>
        {
            new() { Title = "Bad", Detail = "number is invalid" },
            new() { Title = "Also bad" }
        }).Message.Should().Be("Prices service: number is invalid; Also bad");
        new DidwwPricesException(DidwwPricesErrorKind.UnexpectedStatus, 202).Message
            .Should().Be("Prices service responded with status 202");
        new DidwwPricesException(DidwwPricesErrorKind.ConnectionError,
                innerException: new HttpRequestException("connection refused")).Message
            .Should().Be("Prices service: connection refused");
    }

    [Theory]
    [InlineData("<html>maintenance</html>")]
    [InlineData("{}")]
    [InlineData("""{"data":null}""")]
    [InlineData("""{"data":[],"meta":{"total_records":0}}""")]
    [InlineData("""{"data":[{"id":"1","type":"prices","attributes":{}}],"meta":[]}""")]
    public async Task TestPricesLookupUnparsableBody(string body)
    {
        using var server = new PricesServer((_, _) => PricesServer.JsonApi(200, body));
        var client = server.NewClient();

        var act = () => client.VoiceOutPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "442071234567" });

        (await act.Should().ThrowAsync<DidwwClientException>()).Which.Message.Should().StartWith("Failed to parse price lookup");
        server.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task TestPricesLookupWithoutMeta()
    {
        using var server = new PricesServer((_, _) =>
            PricesServer.JsonApi(200, """{"data":[{"id":"1","type":"prices","attributes":{"prefix":"49"}}]}"""));
        var client = server.NewClient();

        var result = await client.VoiceOutPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "4930" });

        result.Rows.Should().ContainSingle().Which.Prefix.Should().Be("49");
        result.TotalRecords.Should().Be(0);
    }

    [Fact]
    public async Task TestPricesServicesCallTheirOwnHost()
    {
        using var server = new PricesServer((_, request) => PricesServer.PriceFile(request, PricesServer.VoiceInFixture()));
        var client = server.NewClient(builder => builder
            .SetVoiceInPricesUrl("https://vin.example.test")
            .SetVoiceOutPricesUrl("https://vout.example.test/v3")
            .SetSmsPricesUrl("https://sms.example.test/v3/"));

        await client.VoiceInPrices().HeadAsync();
        await client.VoiceOutPrices().HeadAsync();
        await client.SmsOutA2PPrices().HeadAsync();
        await client.SmsOutP2PPrices().HeadAsync();
        await client.SmsOutOtpPrices().HeadAsync();
        await client.SmsInPrices().HeadAsync();

        server.Requests.Select(r => r.Uri.ToString()).Should().Equal(
            "https://vin.example.test/v3/prices.csv.gz",
            "https://vout.example.test/v3/prices.csv.gz",
            "https://sms.example.test/v3/sms_out/a2p/prices.csv.gz",
            "https://sms.example.test/v3/sms_out/p2p/prices.csv.gz",
            "https://sms.example.test/v3/sms_out/otp/prices.csv.gz",
            "https://sms.example.test/v3/sms_in/prices.csv.gz");
        server.Requests.Select(r => r.Method).Should().AllBe("HEAD");
        server.MintAudiences().Should().Equal("api-vin.didww.com", "api-vout.didww.com", "api-sms.didww.com");
    }
}
