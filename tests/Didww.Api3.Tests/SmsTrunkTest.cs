using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using Didww.Api3.Resource.Enums;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;

namespace Didww.Api3.Tests;

public class SmsTrunkTest : BaseTest
{
    private const string HttpInId = "1a2b3c4d-0002-4a5b-8c6d-7e8f9a0b1c02";
    private const string HttpOutId = "1a2b3c4d-0003-4a5b-8c6d-7e8f9a0b1c03";
    private const string UnknownId = "1a2b3c4d-0006-4a5b-8c6d-7e8f9a0b1c06";
    private const string GroupId = "2b3c4d5e-0001-4b6c-9d7e-8f9a0b1c2d01";

    private string RequestBody(string method, string path) =>
        WireMock.LogEntries
            .Where(e => e.RequestMessage.Method == method && e.RequestMessage.Path == path)
            .Select(e => e.RequestMessage.Body ?? string.Empty)
            .Single();

    [Fact]
    public async Task TestListSmsTrunks()
    {
        StubGet("sms_trunks", "sms_trunks/index.json");

        var response = await Client.SmsTrunks().ListAsync();
        var trunks = response.Data;

        trunks.Should().HaveCount(5);
        trunks.Select(t => t.Configuration!.ConfigurationType).Should().Equal(
            "sms_to_email", "http_in", "http_out", "smpp_esme", "smpp_smsc");

        var email = trunks[0];
        email.Id.Should().Be("1a2b3c4d-0001-4a5b-8c6d-7e8f9a0b1c01");
        email.Name.Should().Be("sms_to_email trunk");
        email.ExternalReferenceId.Should().BeNull();
        email.Blocked.Should().BeFalse();
        email.Priority.Should().Be(100);
        email.AllowAnyDidAsSource.Should().BeFalse();
        email.AllowIn.Should().BeTrue();
        email.AllowOut.Should().BeFalse();
        email.CreatedAt.Should().Be(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero));
        email.UpdatedAt.Should().Be(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero));
        var emailConfig = email.Configuration.Should().BeOfType<SmsToEmailConfiguration>().Subject;
        emailConfig.SendTo.Should().Be("sms@example.com");
        emailConfig.Subject.Should().Be("Inbound SMS");
        emailConfig.Message.Should().Be("You have a new message.");

        var httpIn = trunks[1].Configuration.Should().BeOfType<HttpInConfiguration>().Subject;
        httpIn.Url.Should().Be("https://example.com/sms");
        httpIn.HttpMethod.Should().Be(SmsHttpMethod.Post);
        httpIn.QueryParameters.Should().BeNull();
        httpIn.Headers.Should().ContainKey("X-Source");
        httpIn.Headers!["X-Source"].Should().Be("didww");
        httpIn.BodyType.Should().Be(SmsBodyType.Json);
        httpIn.Body.Should().Be("{\"text\":\"{SMS_TEXT}\",\"from\":\"{SMS_SRC_ADDR}\"}");
        httpIn.FollowRedirect.Should().BeFalse();
        httpIn.ValidateHttpsCertificate.Should().BeTrue();

        var outTrunk = trunks[2];
        outTrunk.Priority.Should().BeNull();
        outTrunk.AllowOut.Should().BeTrue();
        outTrunk.Host.Should().Be("https://sms-out.example.com/outbound_messages");
        var httpOut = outTrunk.Configuration.Should().BeOfType<HttpOutConfiguration>().Subject;
        httpOut.CallbackUrl.Should().Be("https://example.com/sms/status");
        httpOut.AllowedIps.Should().Equal("203.0.113.10");
        httpOut.AuthName.Should().Be("generated-name");
        httpOut.AuthPassword.Should().Be("generated-password");

        var esme = trunks[3].Configuration.Should().BeOfType<SmppEsmeConfiguration>().Subject;
        esme.SystemType.Should().BeNull();
        esme.AllowedIps.Should().Equal("203.0.113.10");
        esme.ServerLocation.Should().Be(SmppServerLocations.Nyc);
        esme.SystemId.Should().Be("esme1234");
        esme.Password.Should().Be("gen12345");

        var smsc = trunks[4].Configuration.Should().BeOfType<SmppSmscConfiguration>().Subject;
        smsc.Host.Should().Be("smsc.example.com");
        smsc.TxPort.Should().Be(2775);
        smsc.RxPort.Should().Be(2776);
        smsc.TransceiverMode.Should().BeTrue();
        smsc.ConnectionTimeout.Should().Be(30);
        smsc.UseSsl.Should().BeFalse();
        smsc.SystemId.Should().Be("didww");
        smsc.SystemType.Should().BeNull();
        smsc.Password.Should().Be("secret");
        smsc.ServerLocation.Should().Be(SmppServerLocations.Nyc);
    }

    [Fact]
    public async Task TestListSmsTrunksWithIncludedSmsTrunkGroup()
    {
        StubGet("sms_trunks", "sms_trunks/index_with_sms_trunk_group.json");

        var queryParams = new QueryParams()
            .Filter("configuration.type", "http_in")
            .Include("sms_trunk_group")
            .Sort("-priority");
        var response = await Client.SmsTrunks().ListAsync(queryParams);
        var trunks = response.Data;

        var url = Uri.UnescapeDataString(
            WireMock.LogEntries.Single(e => e.RequestMessage.Path == "/v3/sms_trunks").RequestMessage.Url);
        url.Should().Contain("filter[configuration.type]=http_in");
        url.Should().Contain("include=sms_trunk_group");

        trunks.Should().HaveCount(1);
        trunks[0].SmsTrunkGroup.Should().NotBeNull();
        trunks[0].SmsTrunkGroup!.Id.Should().Be(GroupId);
        trunks[0].SmsTrunkGroup!.Name.Should().Be("Inbound SMS");
    }

    [Fact]
    public async Task TestFindSmsTrunk()
    {
        StubGet("sms_trunks/" + HttpOutId, "sms_trunks/show.json");

        var response = await Client.SmsTrunks().FindAsync(HttpOutId);
        var trunk = response.Data;

        trunk.Id.Should().Be(HttpOutId);
        trunk.Name.Should().Be("http_out trunk");
        trunk.Host.Should().Be("https://sms-out.example.com/outbound_messages");
        trunk.Meta.Should().ContainKey("host");
        var config = trunk.Configuration.Should().BeOfType<HttpOutConfiguration>().Subject;
        config.AuthPassword.Should().Be("generated-password");
    }

    [Fact]
    public async Task TestFindSmsTrunkWithIncludedSenderIdVerifications()
    {
        StubGet("sms_trunks/" + HttpOutId, "sms_trunks/show_with_sender_id_verifications.json");

        var queryParams = new QueryParams().Include("sender_id_verifications");
        var response = await Client.SmsTrunks().FindAsync(HttpOutId, queryParams);
        var trunk = response.Data;

        trunk.SenderIdVerifications.Should().HaveCount(1);
        var verification = trunk.SenderIdVerifications![0];
        verification.Id.Should().Be("e5a4e2f1-3b26-4a5c-9a0a-2f7c1f3e8d10");
        verification.Name.Should().Be("ACME alerts");
        verification.Status.Should().Be(SenderIdVerificationStatus.Active);
        verification.SenderType.Should().Be(SenderType.LongCode);
    }

    [Fact]
    public async Task TestFindSmsTrunkNotFound()
    {
        StubGet("sms_trunks/" + HttpOutId, "sms_trunks/not_found.json", 404);

        var act = () => Client.SmsTrunks().FindAsync(HttpOutId);

        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(404);
        ex.Which.Errors.Should().HaveCount(1);
        ex.Which.Errors[0].Title.Should().Be("Record not found");
    }

    [Fact]
    public async Task TestCreateSmsToEmailTrunk()
    {
        StubPost("sms_trunks", "sms_trunks/create_sms_to_email_request.json", "sms_trunks/create_sms_to_email.json");

        var trunk = new SmsTrunk
        {
            Name = "sms_to_email trunk",
            Configuration = new SmsToEmailConfiguration
            {
                SendTo = "sms@example.com",
                Subject = "Inbound SMS",
                Message = "You have a new message."
            }
        };

        var created = (await Client.SmsTrunks().CreateAsync(trunk)).Data;

        created.Id.Should().NotBeNullOrEmpty();
        created.Name.Should().Be("sms_to_email trunk");
        created.Configuration.Should().BeOfType<SmsToEmailConfiguration>();
    }

    [Fact]
    public async Task TestCreateHttpInTrunk()
    {
        StubPost("sms_trunks", "sms_trunks/create_http_in_request.json", "sms_trunks/create_http_in.json");

        var trunk = new SmsTrunk
        {
            Name = "http_in trunk",
            Configuration = new HttpInConfiguration
            {
                Url = "https://example.com/sms",
                HttpMethod = SmsHttpMethod.Post,
                Headers = new Dictionary<string, object> { ["X-Source"] = "didww" },
                BodyType = SmsBodyType.Json,
                Body = "{\"text\":\"{SMS_TEXT}\",\"from\":\"{SMS_SRC_ADDR}\"}",
                FollowRedirect = false,
                ValidateHttpsCertificate = true
            }
        };

        var created = (await Client.SmsTrunks().CreateAsync(trunk)).Data;

        created.Id.Should().Be(HttpInId);
        created.Name.Should().Be("http_in trunk");
        created.Configuration.Should().BeOfType<HttpInConfiguration>();
    }

    [Fact]
    public async Task TestCreateHttpOutTrunkDoesNotSendCredentials()
    {
        StubPost("sms_trunks", "sms_trunks/create_http_out_request.json", "sms_trunks/create_http_out.json");

        var trunk = new SmsTrunk
        {
            Name = "http_out trunk",
            Configuration = new HttpOutConfiguration
            {
                CallbackUrl = "https://example.com/sms/status",
                AllowedIps = new List<string> { "203.0.113.10" },
                // Read-only credentials are never sent.
                AuthName = "ignored-name",
                AuthPassword = "ignored-password"
            }
        };

        var created = (await Client.SmsTrunks().CreateAsync(trunk)).Data;

        created.Name.Should().Be("http_out trunk");
        created.Configuration.Should().BeOfType<HttpOutConfiguration>();
        RequestBody("POST", "/v3/sms_trunks").Should().NotContain("ignored");
    }

    [Fact]
    public async Task TestCreateSmppEsmeTrunkDoesNotSendCredentials()
    {
        StubPost("sms_trunks", "sms_trunks/create_smpp_esme_request.json", "sms_trunks/create_smpp_esme.json");

        var trunk = new SmsTrunk
        {
            Name = "smpp_esme trunk",
            Configuration = new SmppEsmeConfiguration
            {
                AllowedIps = new List<string> { "203.0.113.10" },
                ServerLocation = SmppServerLocations.Nyc,
                // Read-only credentials are never sent.
                SystemId = "ignored-id",
                Password = "ignored-password"
            }
        };

        var created = (await Client.SmsTrunks().CreateAsync(trunk)).Data;

        created.Name.Should().Be("smpp_esme trunk");
        created.Configuration.Should().BeOfType<SmppEsmeConfiguration>();
        RequestBody("POST", "/v3/sms_trunks").Should().NotContain("ignored");
    }

    private sealed class CustomHttpOutConfiguration : HttpOutConfiguration;

    private sealed class CustomSmppEsmeConfiguration : SmppEsmeConfiguration;

    [Fact]
    public async Task TestCreateTrunkWithDerivedConfigurationDoesNotSendCredentials()
    {
        StubPost("sms_trunks", "sms_trunks/create_http_out_request.json", "sms_trunks/create_http_out.json");
        StubPost("sms_trunks", "sms_trunks/create_smpp_esme_request.json", "sms_trunks/create_smpp_esme.json");

        await Client.SmsTrunks().CreateAsync(new SmsTrunk
        {
            Name = "http_out trunk",
            Configuration = new CustomHttpOutConfiguration
            {
                CallbackUrl = "https://example.com/sms/status",
                AllowedIps = new List<string> { "203.0.113.10" },
                AuthName = "ignored-name",
                AuthPassword = "ignored-password"
            }
        });
        await Client.SmsTrunks().CreateAsync(new SmsTrunk
        {
            Name = "smpp_esme trunk",
            Configuration = new CustomSmppEsmeConfiguration
            {
                AllowedIps = new List<string> { "203.0.113.10" },
                ServerLocation = SmppServerLocations.Nyc,
                SystemId = "ignored-id",
                Password = "ignored-password"
            }
        });

        var bodies = WireMock.LogEntries
            .Where(e => e.RequestMessage.Method == "POST" && e.RequestMessage.Path == "/v3/sms_trunks")
            .Select(e => e.RequestMessage.Body ?? "")
            .ToList();
        bodies.Should().HaveCount(2);
        bodies.Should().AllSatisfy(body => body.Should().NotContain("ignored"));
    }

    [Fact]
    public async Task TestCreateSmppSmscTrunk()
    {
        StubPost("sms_trunks", "sms_trunks/create_smpp_smsc_request.json", "sms_trunks/create_smpp_smsc.json");

        var trunk = new SmsTrunk
        {
            Name = "smpp_smsc trunk",
            Configuration = new SmppSmscConfiguration
            {
                Host = "smsc.example.com",
                TxPort = 2775,
                RxPort = 2776,
                TransceiverMode = true,
                ConnectionTimeout = 30,
                UseSsl = false,
                SystemId = "didww",
                Password = "secret",
                ServerLocation = SmppServerLocations.Nyc
            }
        };

        var created = (await Client.SmsTrunks().CreateAsync(trunk)).Data;

        created.Name.Should().Be("smpp_smsc trunk");
        created.Configuration.Should().BeOfType<SmppSmscConfiguration>();
    }

    [Fact]
    public async Task TestCreateSmsTrunkWithGroupAndOutgoingDids()
    {
        StubPost("sms_trunks", "sms_trunks/create_with_relationships_request.json", "sms_trunks/create_smpp_smsc.json");

        var trunk = new SmsTrunk
        {
            Name = "smpp_smsc trunk",
            Configuration = new SmppSmscConfiguration { Host = "smsc.example.com" },
            SmsTrunkGroup = SmsTrunkGroup.Build(GroupId),
            OutgoingDids = new List<Did> { Did.Build("9df99644-f1a5-4a3c-99a4-559d758eb96b") }
        };

        var created = (await Client.SmsTrunks().CreateAsync(trunk)).Data;

        created.Name.Should().Be("smpp_smsc trunk");
    }

    [Fact]
    public async Task TestCreateSmsTrunkError()
    {
        StubPost("sms_trunks", "sms_trunks/create_error_request.json", "sms_trunks/create_error.json", 422);

        var trunk = new SmsTrunk
        {
            Name = "sms_to_email trunk",
            Configuration = new SmsToEmailConfiguration()
        };

        var act = () => Client.SmsTrunks().CreateAsync(trunk);

        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(422);
        ex.Which.Errors.Should().HaveCount(1);
        ex.Which.Errors[0].Detail.Should().Be("configuration - send_to can't be blank");
        ex.Which.Errors[0].Source!["pointer"].ToString().Should().Be("/data/attributes/configuration");
    }

    [Fact]
    public async Task TestUpdateSmsTrunkNameAndConfiguration()
    {
        StubPatch("sms_trunks/" + HttpInId, "sms_trunks/update_http_in_request.json", "sms_trunks/update_http_in.json");

        var trunk = SmsTrunk.Build(HttpInId);
        trunk.Name = "Webhook";
        trunk.Configuration = new HttpInConfiguration { Url = "https://example.com/sms/v2" };

        var updated = (await Client.SmsTrunks().UpdateAsync(trunk)).Data;

        updated.Name.Should().Be("Webhook");
        var config = updated.Configuration.Should().BeOfType<HttpInConfiguration>().Subject;
        config.Url.Should().Be("https://example.com/sms/v2");
        config.HttpMethod.Should().Be(SmsHttpMethod.Post);
    }

    [Fact]
    public async Task TestUpdateLoadedSmsTrunkDoesNotSendCredentials()
    {
        StubGet("sms_trunks/" + HttpOutId, "sms_trunks/show.json");
        StubPatch("sms_trunks/" + HttpOutId, "sms_trunks/update_http_out_request.json", "sms_trunks/update_http_out.json");

        var trunk = (await Client.SmsTrunks().FindAsync(HttpOutId)).Data;
        var config = (HttpOutConfiguration)trunk.Configuration!;
        config.CallbackUrl = "https://example.com/sms/status/v2";
        // Changes inside the configuration are sent once the property is reassigned.
        trunk.Configuration = config;

        var updated = (await Client.SmsTrunks().UpdateAsync(trunk)).Data;

        var updatedConfig = updated.Configuration.Should().BeOfType<HttpOutConfiguration>().Subject;
        updatedConfig.CallbackUrl.Should().Be("https://example.com/sms/status/v2");
        updatedConfig.AuthPassword.Should().Be("generated-password");
        RequestBody("PATCH", "/v3/sms_trunks/" + HttpOutId).Should().NotContain("generated");
    }

    [Fact]
    public async Task TestUpdateLoadedSmsTrunkWithoutChangesSendsNothing()
    {
        StubGet("sms_trunks/" + HttpOutId, "sms_trunks/show.json");
        StubPatch("sms_trunks/" + HttpOutId, "sms_trunks/show.json");

        var trunk = (await Client.SmsTrunks().FindAsync(HttpOutId)).Data;
        await Client.SmsTrunks().UpdateAsync(trunk);

        var data = JObject.Parse(RequestBody("PATCH", "/v3/sms_trunks/" + HttpOutId))["data"]!;
        data["attributes"].Should().BeNull();
        data["relationships"].Should().BeNull();
    }

    [Fact]
    public async Task TestUpdateSmsTrunkClearRelationships()
    {
        StubPatch("sms_trunks/" + HttpInId, "sms_trunks/update_clear_relationships_request.json", "sms_trunks/update_http_in.json");

        var trunk = SmsTrunk.Build(HttpInId);
        trunk.OutgoingDids = new List<Did>();
        trunk.SmsTrunkGroup = null;

        await Client.SmsTrunks().UpdateAsync(trunk);

        var relationships = JObject.Parse(RequestBody("PATCH", "/v3/sms_trunks/" + HttpInId))
            .SelectToken("data.relationships")!;
        relationships["sms_trunk_group"]!.ToString(Newtonsoft.Json.Formatting.None).Should().Be("{\"data\":null}");
        relationships.SelectToken("outgoing_dids.data").Should().BeOfType<JArray>().Which.Should().BeEmpty();
    }

    [Fact]
    public async Task TestUnknownConfigurationIsPreservedWithExactNumbers()
    {
        StubGet("sms_trunks/" + UnknownId, "sms_trunks/show_unknown_configuration.json");
        StubPatch("sms_trunks/" + UnknownId, "sms_trunks/update_unknown_configuration_request.json",
            "sms_trunks/show_unknown_configuration.json");

        var trunk = (await Client.SmsTrunks().FindAsync(UnknownId)).Data;

        var generic = trunk.Configuration.Should().BeOfType<GenericSmsTrunkConfiguration>().Subject;
        generic.ConfigurationType.Should().Be("future_transport");
        generic.Attributes["endpoint"]!.Value<string>().Should().Be("https://example.com/future");
        generic.Attributes["retries"]!.Value<long>().Should().Be(3);
        generic.Attributes["enabled"]!.Value<bool>().Should().BeTrue();

        generic.Attributes["endpoint"] = "https://example.com/future/v2";
        trunk.Configuration = generic;
        await Client.SmsTrunks().UpdateAsync(trunk);

        // The JSON matcher compares parsed values; the raw body proves the
        // numbers are written back exactly as received.
        var body = RequestBody("PATCH", "/v3/sms_trunks/" + UnknownId);
        body.Should().Contain("\"ratio\":1.10");
        body.Should().Contain("\"sequence\":9007199254740993");
        body.Should().Contain("[123456789012345678901234567890]");
        body.Should().Contain("\"cleared\":null");
    }

    [Fact]
    public async Task TestUpdateGenericConfigurationSendsNulls()
    {
        StubPatch("sms_trunks/" + HttpInId, "sms_trunks/update_generic_nulls_request.json", "sms_trunks/update_http_in.json");

        var trunk = SmsTrunk.Build(HttpInId);
        trunk.Configuration = new GenericSmsTrunkConfiguration("http_in", new JObject
        {
            ["http_method"] = "get",
            ["body_type"] = JValue.CreateNull(),
            ["body"] = JValue.CreateNull()
        });

        await Client.SmsTrunks().UpdateAsync(trunk);

        var attributes = JObject.Parse(RequestBody("PATCH", "/v3/sms_trunks/" + HttpInId))
            .SelectToken("data.attributes.configuration.attributes")!;
        attributes["http_method"]!.Value<string>().Should().Be("get");
        attributes["body_type"]!.Type.Should().Be(JTokenType.Null);
        attributes["body"]!.Type.Should().Be(JTokenType.Null);
    }

    [Fact]
    public async Task TestDeleteSmsTrunk()
    {
        StubDelete("sms_trunks/" + HttpInId);

        await Client.SmsTrunks().DeleteAsync(HttpInId);

        RequestsTo("/v3/sms_trunks/" + HttpInId).Should().ContainSingle().Which.Method.Should().Be("DELETE");
    }

    [Fact]
    public async Task TestDeleteSmsTrunkInUse()
    {
        WireMock.Given(
            Request.Create().WithPath("/v3/sms_trunks/" + HttpInId).UsingDelete()
        ).RespondWith(
            Response.Create()
                .WithStatusCode(422)
                .WithHeader("Content-Type", "application/vnd.api+json")
                .WithBody(LoadFixture("sms_trunks/delete_error.json"))
        );

        var act = () => Client.SmsTrunks().DeleteAsync(HttpInId);

        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(422);
        ex.Which.Errors[0].Detail.Should().Be("base - Cannot delete an SMS trunk while DIDs are routed to it");
    }

    [Fact]
    public void TestHttpOutConfigurationToStringRedactsCredentials()
    {
        var config = new HttpOutConfiguration
        {
            CallbackUrl = "https://example.com/status",
            AuthName = "generated-name",
            AuthPassword = "generated-password"
        };

        var text = config.ToString();

        text.Should().Contain("https://example.com/status");
        text.Should().Contain("[FILTERED]");
        text.Should().NotContain("generated-name");
        text.Should().NotContain("generated-password");
    }

    [Fact]
    public void TestSmppEsmeConfigurationToStringRedactsCredentials()
    {
        var config = new SmppEsmeConfiguration
        {
            ServerLocation = SmppServerLocations.Nyc,
            SystemId = "esme1234",
            Password = "gen12345"
        };

        var text = config.ToString();

        text.Should().Contain("nyc");
        text.Should().Contain("[FILTERED]");
        text.Should().NotContain("esme1234");
        text.Should().NotContain("gen12345");
    }

    [Fact]
    public void TestSmppSmscConfigurationToStringRedactsPassword()
    {
        var config = new SmppSmscConfiguration
        {
            Host = "smsc.example.com",
            SystemId = "didww",
            Password = "secret"
        };

        var text = config.ToString();

        text.Should().Contain("smsc.example.com");
        text.Should().Contain("didww");
        text.Should().Contain("[FILTERED]");
        text.Should().NotContain("secret");
    }

    [Fact]
    public void TestHttpInConfigurationToStringRedactsHeaders()
    {
        var config = new HttpInConfiguration
        {
            Url = "https://example.com/sms",
            Headers = new Dictionary<string, object> { ["Authorization"] = "Bearer abc123" }
        };

        var text = config.ToString();

        text.Should().Contain("https://example.com/sms");
        text.Should().Contain("[FILTERED]");
        text.Should().NotContain("Authorization");
        text.Should().NotContain("Bearer abc123");
    }

    [Fact]
    public void TestGenericConfigurationToStringRedactsEveryValue()
    {
        var config = new GenericSmsTrunkConfiguration("smpp_smsc", new JObject
        {
            ["host"] = "smsc.example.com",
            ["password"] = "secret",
            ["tx_port"] = 2775,
            ["headers"] = new JObject { ["Authorization"] = "Bearer abc123" },
            ["system_type"] = JValue.CreateNull()
        });

        var text = config.ToString();

        text.Should().Contain("smpp_smsc");
        text.Should().Contain("host=[FILTERED]");
        text.Should().Contain("password=[FILTERED]");
        text.Should().Contain("tx_port=[FILTERED]");
        text.Should().Contain("headers=[FILTERED]");
        text.Should().Contain("system_type=null");
        text.Should().NotContain("smsc.example.com");
        text.Should().NotContain("secret");
        text.Should().NotContain("2775");
        text.Should().NotContain("Authorization");
        text.Should().NotContain("Bearer abc123");
    }
}
