using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;
using FluentAssertions;
using JsonApiSerializer;
using Newtonsoft.Json;

namespace Didww.Api3.Tests;

public class VoiceInTrunkTest : BaseTest
{
    [Fact]
    public async Task TestListVoiceInTrunks()
    {
        StubGet("voice_in_trunks", "voice_in_trunks/index.json");

        var queryParams = new QueryParams().Include("voice_in_trunk_group");
        var response = await Client.VoiceInTrunks().ListAsync(queryParams);
        var trunks = response.Data;

        trunks.Should().NotBeEmpty();

        var first = trunks[0];
        first.Id.Should().Be("2b4b1fcf-fe6a-4de9-8a58-7df46820ba13");
        first.Name.Should().Be("sample trunk pstn");
        first.Priority.Should().Be(1);
        first.Weight.Should().Be(65535);
        first.CliFormat.Should().Be(CliFormat.E164);
        first.Pop.Should().BeNull();
        trunks[1].Pop.Should().Be(Pop.Fra);
    }

    [Fact]
    public async Task TestListSipConfigurationAttributes()
    {
        StubGet("voice_in_trunks", "voice_in_trunks/index.json");

        var response = await Client.VoiceInTrunks().ListAsync();
        var trunks = response.Data;

        var sipTrunk = trunks.FirstOrDefault(t => t.Configuration is SipConfiguration);
        sipTrunk.Should().NotBeNull();

        var config = (SipConfiguration)sipTrunk!.Configuration!;
        config.Username.Should().Be("username");
        config.Host.Should().Be("203.0.113.78");
        config.Port.Should().Be(8060);
        config.CodecIds.Should().ContainInOrder(Codec.PCMU, Codec.PCMA, Codec.G729);
        config.TransportProtocolId.Should().Be(TransportProtocol.UDP);
        config.AuthEnabled.Should().BeTrue();
        config.AuthUser.Should().Be("auth_user");
        config.AuthPassword.Should().Be("auth_password");
        config.ResolveRuri.Should().BeTrue();
        config.RxDtmfFormatId.Should().Be(RxDtmfFormat.RFC_2833);
        config.TxDtmfFormatId.Should().Be(TxDtmfFormat.Disabled);
        config.SstEnabled.Should().BeFalse();
        config.SstMinTimer.Should().Be(600);
        config.SstMaxTimer.Should().Be(900);
        config.SstAccept501.Should().BeTrue();
        config.SstRefreshMethodId.Should().Be(SstRefreshMethod.INVITE);
        config.SipTimerB.Should().Be(8000);
        config.DnsSrvFailoverTimer.Should().Be(2000);
        config.RtpPing.Should().BeFalse();
        config.ForceSymmetricRtp.Should().BeFalse();
        config.MaxTransfers.Should().Be(2);
        config.Max30xRedirects.Should().Be(5);
        config.MediaEncryptionMode.Should().Be(MediaEncryptionMode.Disabled);
        config.StirShakenMode.Should().Be(StirShakenMode.Disabled);
        config.AllowedRtpIps.Should().BeNull();
        config.DiversionRelayPolicy.Should().Be(DiversionRelayPolicy.AsIs);
        config.ReroutingDisconnectCodeIds.Should().BeNull();
    }

    [Fact]
    public async Task TestCreateVoiceInTrunk()
    {
        StubPost("voice_in_trunks", "voice_in_trunks/create_request.json", "voice_in_trunks/create.json");

        var config = new PstnConfiguration { Dst = "558540420024" };

        var trunk = new VoiceInTrunk
        {
            Name = "hello, test pstn trunk",
            Configuration = config
        };

        var response = await Client.VoiceInTrunks().CreateAsync(trunk);
        var created = response.Data;

        created.Id.Should().Be("41b94706-325e-4704-a433-d65105758836");
        created.Name.Should().Be("hello, test pstn trunk");
    }

    [Fact]
    public async Task TestUpdatePstnTrunk()
    {
        StubPatch("voice_in_trunks/41b94706-325e-4704-a433-d65105758836", "voice_in_trunks/update_pstn_request.json", "voice_in_trunks/update_pstn.json");

        var config = new PstnConfiguration { Dst = "558540420025" };

        var trunk = VoiceInTrunk.Build("41b94706-325e-4704-a433-d65105758836");
        trunk.Name = "hello, updated test pstn trunk";
        trunk.Configuration = config;

        var response = await Client.VoiceInTrunks().UpdateAsync(trunk);
        var updated = response.Data;

        updated.Id.Should().Be("41b94706-325e-4704-a433-d65105758836");
        updated.Name.Should().Be("hello, updated test pstn trunk");
        updated.Configuration.Should().BeOfType<PstnConfiguration>();
        ((PstnConfiguration)updated.Configuration!).Dst.Should().Be("558540420025");
    }

    [Fact]
    public async Task TestUpdateSipTrunk()
    {
        StubPatch("voice_in_trunks/a80006b6-4183-4865-8b99-7ebbd359a762",
            "voice_in_trunks/update_sip_request.json", "voice_in_trunks/update_sip.json");

        var sipConfig = new SipConfiguration
        {
            Username = "new-username",
            Host = "203.0.113.110",
            Port = 5060,
            CodecIds = new List<Codec> { Codec.PCMU, Codec.PCMA, Codec.G729, Codec.G723, Codec.TELEPHONE_EVENT },
            SstRefreshMethodId = SstRefreshMethod.INVITE,
            MediaEncryptionMode = MediaEncryptionMode.Zrtp,
            StirShakenMode = StirShakenMode.Pai,
            AllowedRtpIps = new List<string> { "203.0.113.1" }
        };

        var trunk = VoiceInTrunk.Build("a80006b6-4183-4865-8b99-7ebbd359a762");
        trunk.Name = "hello, updated test sip trunk";
        trunk.Description = "just a description";
        trunk.Configuration = sipConfig;

        var response = await Client.VoiceInTrunks().UpdateAsync(trunk);
        var updated = response.Data;

        updated.Id.Should().Be("a80006b6-4183-4865-8b99-7ebbd359a762");
        updated.Name.Should().Be("hello, updated test sip trunk");
        updated.Configuration.Should().BeOfType<SipConfiguration>();

        var config = (SipConfiguration)updated.Configuration!;
        config.Username.Should().Be("new-username");
        config.MediaEncryptionMode.Should().Be(MediaEncryptionMode.Zrtp);
        config.StirShakenMode.Should().Be(StirShakenMode.Pai);
    }

    [Fact]
    public async Task TestDeleteVoiceInTrunk()
    {
        var id = "41b94706-325e-4704-a433-d65105758836";
        StubDelete("voice_in_trunks/" + id);

        await Client.VoiceInTrunks().DeleteAsync(id);
    }

    [Fact]
    public async Task TestCreateSipTrunkWithReroutingDisconnectCodes()
    {
        StubPost("voice_in_trunks", "voice_in_trunks/create_sip_with_rerouting_request.json", "voice_in_trunks/create_sip_with_rerouting.json");

        var sipConfig = new SipConfiguration
        {
            Username = "username",
            Host = "203.0.113.110",
            SstRefreshMethodId = SstRefreshMethod.INVITE,
            Port = 5060,
            CodecIds = new List<Codec> { Codec.PCMU, Codec.PCMA, Codec.G729, Codec.G723, Codec.TELEPHONE_EVENT },
            ReroutingDisconnectCodeIds = new List<ReroutingDisconnectCode>
            {
                ReroutingDisconnectCode.SIP_400_BAD_REQUEST,
                ReroutingDisconnectCode.SIP_402_PAYMENT_REQUIRED,
                ReroutingDisconnectCode.SIP_403_FORBIDDEN,
                ReroutingDisconnectCode.SIP_404_NOT_FOUND,
                ReroutingDisconnectCode.SIP_408_REQUEST_TIMEOUT,
                ReroutingDisconnectCode.SIP_409_CONFLICT,
                ReroutingDisconnectCode.SIP_410_GONE,
                ReroutingDisconnectCode.SIP_412_CONDITIONAL_REQUEST_FAILED,
                ReroutingDisconnectCode.SIP_413_REQUEST_ENTITY_TOO_LARGE,
                ReroutingDisconnectCode.SIP_414_REQUEST_URI_TOO_LONG,
                ReroutingDisconnectCode.SIP_415_UNSUPPORTED_MEDIA_TYPE,
                ReroutingDisconnectCode.SIP_416_UNSUPPORTED_URI_SCHEME,
                ReroutingDisconnectCode.SIP_417_UNKNOWN_RESOURCE_PRIORITY,
                ReroutingDisconnectCode.SIP_420_BAD_EXTENSION,
                ReroutingDisconnectCode.SIP_421_EXTENSION_REQUIRED,
                ReroutingDisconnectCode.SIP_422_SESSION_INTERVAL_TOO_SMALL,
                ReroutingDisconnectCode.SIP_423_INTERVAL_TOO_BRIEF,
                ReroutingDisconnectCode.SIP_424_BAD_LOCATION_INFORMATION,
                ReroutingDisconnectCode.SIP_428_USE_IDENTITY_HEADER,
                ReroutingDisconnectCode.SIP_429_PROVIDE_REFERRER_IDENTITY,
                ReroutingDisconnectCode.SIP_433_ANONYMITY_DISALLOWED,
                ReroutingDisconnectCode.SIP_436_BAD_IDENTITY_INFO,
                ReroutingDisconnectCode.SIP_437_UNSUPPORTED_CERTIFICATE,
                ReroutingDisconnectCode.SIP_438_INVALID_IDENTITY_HEADER,
                ReroutingDisconnectCode.SIP_480_TEMPORARILY_UNAVAILABLE,
                ReroutingDisconnectCode.SIP_482_LOOP_DETECTED,
                ReroutingDisconnectCode.SIP_483_TOO_MANY_HOPS,
                ReroutingDisconnectCode.SIP_484_ADDRESS_INCOMPLETE,
                ReroutingDisconnectCode.SIP_485_AMBIGUOUS,
                ReroutingDisconnectCode.SIP_486_BUSY_HERE,
                ReroutingDisconnectCode.SIP_487_REQUEST_TERMINATED,
                ReroutingDisconnectCode.SIP_488_NOT_ACCEPTABLE_HERE,
                ReroutingDisconnectCode.SIP_494_SECURITY_AGREEMENT_REQUIRED,
                ReroutingDisconnectCode.SIP_500_SERVER_INTERNAL_ERROR,
                ReroutingDisconnectCode.SIP_501_NOT_IMPLEMENTED,
                ReroutingDisconnectCode.SIP_502_BAD_GATEWAY,
                ReroutingDisconnectCode.SIP_504_SERVER_TIME_OUT,
                ReroutingDisconnectCode.SIP_505_VERSION_NOT_SUPPORTED,
                ReroutingDisconnectCode.SIP_513_MESSAGE_TOO_LARGE,
                ReroutingDisconnectCode.SIP_580_PRECONDITION_FAILURE,
                ReroutingDisconnectCode.SIP_600_BUSY_EVERYWHERE,
                ReroutingDisconnectCode.SIP_603_DECLINE,
                ReroutingDisconnectCode.SIP_604_DOES_NOT_EXIST_ANYWHERE,
                ReroutingDisconnectCode.SIP_606_NOT_ACCEPTABLE,
                ReroutingDisconnectCode.RINGING_TIMEOUT
            },
            MediaEncryptionMode = MediaEncryptionMode.Zrtp,
            StirShakenMode = StirShakenMode.Pai,
            AllowedRtpIps = new List<string> { "203.0.113.1" },
            // API 2026-04-16 writable attributes
            DiversionRelayPolicy = DiversionRelayPolicy.AsIs,
            DiversionInjectMode = DiversionInjectMode.DidNumber,
            NetworkProtocolPriority = NetworkProtocolPriority.ForceIpv4,
            CnamLookup = true,
            // use_did_in_ruri must stay false unless EnabledSipRegistration
            // is also true (server returns 422 otherwise).  Setting it here
            // is redundant against the default but documents the field.
            UseDidInRuri = false,
        };

        var trunk = new VoiceInTrunk
        {
            Name = "hello, test sip trunk",
            Configuration = sipConfig
        };

        var response = await Client.VoiceInTrunks().CreateAsync(trunk);
        var created = response.Data;
        created.Configuration.Should().BeOfType<SipConfiguration>();

        var config = (SipConfiguration)created.Configuration!;
        config.ReroutingDisconnectCodeIds.Should().HaveCount(45);
        config.ReroutingDisconnectCodeIds![0].Should().Be(ReroutingDisconnectCode.SIP_400_BAD_REQUEST);
        config.ReroutingDisconnectCodeIds[^1].Should().Be(ReroutingDisconnectCode.RINGING_TIMEOUT);
        config.ReroutingDisconnectCodeIds.Should().Contain(ReroutingDisconnectCode.SIP_480_TEMPORARILY_UNAVAILABLE);
    }

    [Fact]
    public async Task TestListSipRegistrationAttributesNoRegistration()
    {
        StubGet("voice_in_trunks", "voice_in_trunks/index.json");

        var response = await Client.VoiceInTrunks().ListAsync();
        var sipTrunk = response.Data.FirstOrDefault(t => t.Configuration is SipConfiguration);
        sipTrunk.Should().NotBeNull();

        var config = (SipConfiguration)sipTrunk!.Configuration!;
        config.DiversionInjectMode.Should().Be(DiversionInjectMode.None);
        config.NetworkProtocolPriority.Should().Be(NetworkProtocolPriority.Any);
        config.EnabledSipRegistration.Should().BeFalse();
        config.UseDidInRuri.Should().BeFalse();
        config.CnamLookup.Should().BeFalse();
        config.IncomingAuthUsername.Should().BeNull();
        config.IncomingAuthPassword.Should().BeNull();
    }

    [Fact]
    public async Task TestShowSipRegistrationEnabledIncludesIncomingAuth()
    {
        var id = "f1c5d834-1d1f-49cc-8e88-3f73c0a35b31";
        StubGet($"voice_in_trunks/{id}", "voice_in_trunks/show_sip_registration.json");

        var response = await Client.VoiceInTrunks().FindAsync(id);
        var trunk = response.Data;

        var config = (SipConfiguration)trunk.Configuration!;
        config.EnabledSipRegistration.Should().BeTrue();
        config.UseDidInRuri.Should().BeTrue();
        config.CnamLookup.Should().BeTrue();
        config.DiversionInjectMode.Should().Be(DiversionInjectMode.DidNumber);
        config.NetworkProtocolPriority.Should().Be(NetworkProtocolPriority.PreferIpv4);
        config.IncomingAuthUsername.Should().Be("srv_generated_user");
        config.IncomingAuthPassword.Should().Be("srv_generated_pass");
    }

    [Fact]
    public async Task TestCreateSipRegistrationTrunkSerializesWritableAttrsOnly()
    {
        StubPost("voice_in_trunks",
            "voice_in_trunks/create_sip_registration_request.json",
            "voice_in_trunks/show_sip_registration.json");

        // Note: Username/Host/Port are intentionally NOT set — the server
        // requires them blank when EnabledSipRegistration is true (returns
        // 422 otherwise). Verified against sandbox.
        var sipConfig = new SipConfiguration
        {
            EnabledSipRegistration = true,
            UseDidInRuri = true,
            CnamLookup = true,
            DiversionInjectMode = DiversionInjectMode.DidNumber,
            NetworkProtocolPriority = NetworkProtocolPriority.PreferIpv4
        };

        var trunk = new VoiceInTrunk
        {
            Name = "sip registration trunk",
            Configuration = sipConfig
        };

        var response = await Client.VoiceInTrunks().CreateAsync(trunk);
        response.Data.Id.Should().Be("f1c5d834-1d1f-49cc-8e88-3f73c0a35b31");

        // The server returns 201 with server-generated incoming_auth_*
        // credentials. The SDK must surface those populated values to the
        // caller (NOT null) so users can wire them into their endpoints.
        var created = (SipConfiguration)response.Data.Configuration!;
        created.IncomingAuthUsername.Should().NotBeNullOrEmpty();
        created.IncomingAuthPassword.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TestIncomingAuthFieldsAreNotSerialized()
    {
        // Simulate a SipConfiguration loaded from a server response with
        // populated incoming_auth_* values, then serialize it standalone
        // and assert those keys never appear in the output.
        var json = """
        {
            "type": "sip_configurations",
            "attributes": {
                "username": "u",
                "host": "203.0.113.1",
                "enabled_sip_registration": true,
                "incoming_auth_username": "srv_user",
                "incoming_auth_password": "srv_pass"
            }
        }
        """;

        var token = Newtonsoft.Json.Linq.JToken.Parse(json);
        var attrs = token["attributes"]!;
        var serializer = JsonSerializer.CreateDefault();
        var loaded = attrs.ToObject<SipConfiguration>(serializer)!;

        loaded.IncomingAuthUsername.Should().Be("srv_user");
        loaded.IncomingAuthPassword.Should().Be("srv_pass");

        var output = JsonConvert.SerializeObject(loaded);
        output.Should().NotContain("incoming_auth_username");
        output.Should().NotContain("incoming_auth_password");
    }

    [Fact]
    public async Task TestRoundTripPatchDoesNotEchoIncomingAuth()
    {
        var id = "f1c5d834-1d1f-49cc-8e88-3f73c0a35b31";

        // Step 1: load the trunk via GET — the resulting SipConfiguration
        // has incoming_auth_username / incoming_auth_password populated.
        StubGet($"voice_in_trunks/{id}", "voice_in_trunks/show_sip_registration.json");
        var loaded = await Client.VoiceInTrunks().FindAsync(id);
        var loadedConfig = (SipConfiguration)loaded.Data.Configuration!;
        loadedConfig.IncomingAuthUsername.Should().NotBeNull();
        loadedConfig.IncomingAuthPassword.Should().NotBeNull();

        // Step 2: PATCH the trunk reusing the loaded configuration.
        StubPatch($"voice_in_trunks/{id}", "voice_in_trunks/show_sip_registration.json");

        var trunk = VoiceInTrunk.Build(id);
        trunk.Configuration = loadedConfig;

        var response = await Client.VoiceInTrunks().UpdateAsync(trunk);
        response.Data.Id.Should().Be(id);

        // Step 3: assert the captured PATCH request body does not contain
        // incoming_auth_username / incoming_auth_password — the API rejects
        // those keys with 400 Param not allowed, so the SDK must strip them.
        var patchRequests = WireMock.LogEntries
            .Where(e => e.RequestMessage.Method == "PATCH")
            .ToList();
        patchRequests.Should().HaveCount(1);
        var body = patchRequests[0].RequestMessage.Body ?? string.Empty;
        body.Should().NotContain("incoming_auth_username");
        body.Should().NotContain("incoming_auth_password");
    }

    [Fact]
    public async Task TestDisableSipRegistrationPatchSerializesAllThreeFields()
    {
        // The disable flow is a multi-field PATCH because the server
        // returns 422 for any request that flips EnabledSipRegistration
        // to false without simultaneously providing a non-blank Host and
        // UseDidInRuri = false.
        // Lock those three fields in the same request body — if EnabledSip-
        // Registration ever becomes a plain `bool`, the explicit `false`
        // will silently drop and this test will fail.
        var id = "57a939dd-1600-41a6-80b1-f624e22a1f4c";
        StubPatch($"voice_in_trunks/{id}",
            "voice_in_trunks/disable_sip_registration_request.json",
            "voice_in_trunks/disable_sip_registration.json");

        var sipConfig = new SipConfiguration
        {
            EnabledSipRegistration = false,
            UseDidInRuri = false,
            Host = "203.0.113.10"
        };

        var trunk = VoiceInTrunk.Build(id);
        trunk.Configuration = sipConfig;

        var response = await Client.VoiceInTrunks().UpdateAsync(trunk);
        var updated = (SipConfiguration)response.Data.Configuration!;
        updated.EnabledSipRegistration.Should().BeFalse();
        updated.UseDidInRuri.Should().BeFalse();
        updated.Host.Should().Be("203.0.113.10");
        updated.IncomingAuthUsername.Should().BeNull();
        updated.IncomingAuthPassword.Should().BeNull();
    }

    [Fact]
    public void TestSerializeNullConfiguration()
    {
        var trunk = new VoiceInTrunk
        {
            Name = "test",
            Configuration = null
        };

        var settings = new JsonApiSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        };
        var json = JsonConvert.SerializeObject(trunk, settings);
        json.Should().NotBeNull();
    }

    [Fact]
    public void TestDeserializeNullConfiguration()
    {
        var json = """
        {
            "data": {
                "id": "abc",
                "type": "voice_in_trunks",
                "attributes": {
                    "name": "test",
                    "configuration": null
                }
            }
        }
        """;

        var settings = new JsonApiSerializerSettings();
        var trunk = JsonConvert.DeserializeObject<VoiceInTrunk>(json, settings);
        trunk.Should().NotBeNull();
        trunk!.Configuration.Should().BeNull();
    }

    [Fact]
    public void TestEnablingSipRegistrationClearsHostAndPortOnWire()
    {
        // The cascade is wire-time (in SipConfigurationJsonConverter), not
        // setter-side. In-memory the user's struct stays as written; the
        // converter applies the server-required invariant only at serialize.
        var cfg = new SipConfiguration { Host = "sip.example.com", Port = 5060, EnabledSipRegistration = true };
        var json = JsonConvert.SerializeObject(cfg);
        json.Should().Contain("\"host\":null");
        json.Should().Contain("\"port\":null");
        json.Should().Contain("\"enabled_sip_registration\":true");
    }

    [Fact]
    public void TestDisablingSipRegistrationForcesUseDidInRuriToFalseOnWire()
    {
        var cfg = new SipConfiguration { EnabledSipRegistration = false, UseDidInRuri = true };
        var json = JsonConvert.SerializeObject(cfg);
        json.Should().Contain("\"enabled_sip_registration\":false");
        json.Should().Contain("\"use_did_in_ruri\":false");
    }

    [Fact]
    public void TestSettingHostDisablesSipRegistrationAndForcesUseDidInRuriToFalseOnWire()
    {
        // When Host is set without an explicit EnabledSipRegistration=true,
        // the converter forces enabled_sip_registration=false and
        // use_did_in_ruri=false on the wire — matching the server's
        // requirement when host is present.
        var cfg = new SipConfiguration { UseDidInRuri = true, Host = "sip.example.com" };
        var json = JsonConvert.SerializeObject(cfg);
        json.Should().Contain("\"host\":\"sip.example.com\"");
        json.Should().Contain("\"enabled_sip_registration\":false");
        json.Should().Contain("\"use_did_in_ruri\":false");
    }

    [Fact]
    public void TestEnablingSipRegistrationLeavesUseDidInRuriUntouchedOnWire()
    {
        var cfg = new SipConfiguration { EnabledSipRegistration = true, UseDidInRuri = true };
        var json = JsonConvert.SerializeObject(cfg);
        json.Should().Contain("\"enabled_sip_registration\":true");
        json.Should().Contain("\"use_did_in_ruri\":true");
    }

    [Fact]
    public void TestSipConfigurationWirePayloadReflectsCascadedState()
    {
        // Wire-format integration check: Host-driven cascade fires through
        // SipConfigurationJsonConverter (Host non-blank ⇒ enabled=false,
        // useDidInRuri=false), and the read-only credential fields are
        // stripped. EnabledSipRegistration is left null in the input so the
        // Host rule applies — see TestEnablingSipRegistrationClearsHostAnd-
        // PortOnWire for the precedence-of-Enabled case.
        var cfg = new SipConfiguration
        {
            UseDidInRuri = true,
            Host = "sip.example.com",
        };
        var json = JsonConvert.SerializeObject(cfg);
        json.Should().Contain("\"host\":\"sip.example.com\"");
        json.Should().Contain("\"enabled_sip_registration\":false");
        json.Should().Contain("\"use_did_in_ruri\":false");
    }

    [Fact]
    public void TestDeserializingServerResponseDoesNotTriggerCascade()
    {
        // Server-returned shapes (sip_registration enabled with host: null,
        // or sip_registration disabled with host: present) are already
        // consistent — the cascade must not run during deserialization
        // because the property-set order is up to Newtonsoft.Json and
        // would clobber valid combinations.
        var token = Newtonsoft.Json.Linq.JToken.Parse(LoadFixture("voice_in_trunks/sip_regular_load_shape.json"));
        var attrs = token["attributes"]!;
        var serializer = JsonSerializer.CreateDefault();
        var config = attrs.ToObject<SipConfiguration>(serializer)!;
        config.Host.Should().Be("sip.example.com");
        config.Port.Should().Be(5060);
        config.EnabledSipRegistration.Should().BeFalse();
        config.UseDidInRuri.Should().BeTrue("deserialization must not cascade UseDidInRuri to false");
    }

    [Fact]
    public void TestEnablingSipRegistrationOnFreshConfigEmitsHostAndPortAsNullOnWire()
    {
        // Regression: PATCH against an existing trunk that already has a
        // host/port persisted server-side. The local SipConfiguration starts
        // empty (Host/Port never assigned), so the cascade must still emit
        // "host": null and "port": null on the wire — otherwise the server
        // merges the new EnabledSipRegistration=true with the persisted
        // host and rejects with 422.
        var cfg = new SipConfiguration { EnabledSipRegistration = true };
        var json = JsonConvert.SerializeObject(cfg);
        json.Should().Contain("\"host\":null");
        json.Should().Contain("\"port\":null");
        json.Should().Contain("\"enabled_sip_registration\":true");
    }

    [Fact]
    public void TestSipConfigurationToStringRedactsCredentials()
    {
        // Default ToString output is what shows up in default logging /
        // debugger inspection / unhandled exception traces — none of those
        // contexts should ever expose SIP credentials in plaintext.
        var loadedJson = """
        {
            "type": "sip_configurations",
            "attributes": {
                "username": "alice",
                "host": "sip.example.com",
                "auth_password": "s3cret-Pa55",
                "enabled_sip_registration": true,
                "incoming_auth_username": "srv-user-xyz",
                "incoming_auth_password": "srv-pass-xyz"
            }
        }
        """;
        var token = Newtonsoft.Json.Linq.JToken.Parse(loadedJson);
        var attrs = token["attributes"]!;
        var serializer = JsonSerializer.CreateDefault();
        var config = attrs.ToObject<SipConfiguration>(serializer)!;

        var output = config.ToString();
        output.Should().Contain("alice", "non-sensitive Username should still be visible");
        output.Should().Contain("sip.example.com", "non-sensitive Host should still be visible");
        output.Should().NotContain("s3cret-Pa55");
        output.Should().NotContain("srv-user-xyz");
        output.Should().NotContain("srv-pass-xyz");
        output.Should().Contain("[FILTERED]");
    }

    [Fact]
    public async Task TestCreateVoiceInTrunkWithPop()
    {
        StubPost("voice_in_trunks", "voice_in_trunks/create_with_pop_request.json", "voice_in_trunks/create_with_pop.json");

        var trunk = new VoiceInTrunk
        {
            Name = "hello, test pstn trunk",
            Configuration = new PstnConfiguration { Dst = "558540420024" },
            Pop = Pop.Fra
        };

        var created = (await Client.VoiceInTrunks().CreateAsync(trunk)).Data;

        created.Id.Should().Be("41b94706-325e-4704-a433-d65105758836");
        created.Pop.Should().Be(Pop.Fra);
    }

    [Fact]
    public async Task TestCreateVoiceInTrunkOmitsNullPop()
    {
        StubPost("voice_in_trunks", "voice_in_trunks/create.json");

        var trunk = new VoiceInTrunk
        {
            Name = "hello, test pstn trunk",
            Configuration = new PstnConfiguration { Dst = "558540420024" }
        };

        await Client.VoiceInTrunks().CreateAsync(trunk);

        var body = WireMock.LogEntries
            .Single(e => e.RequestMessage.Method == "POST" && e.RequestMessage.Path == "/v3/voice_in_trunks")
            .RequestMessage.Body;
        body.Should().NotContain("\"pop\"");
    }

    [Fact]
    public async Task TestUpdateVoiceInTrunkClearPop()
    {
        StubPatch("voice_in_trunks/41b94706-325e-4704-a433-d65105758836",
            "voice_in_trunks/update_clear_pop_request.json", "voice_in_trunks/create.json");

        var trunk = VoiceInTrunk.Build("41b94706-325e-4704-a433-d65105758836");
        trunk.Pop = null;

        await Client.VoiceInTrunks().UpdateAsync(trunk);
    }

    [Theory]
    [InlineData("\"NYC\"", Pop.Nyc)]
    [InlineData("\"FRA\"", Pop.Fra)]
    [InlineData("\"LAC\"", Pop.Lac)]
    [InlineData("\"MIA\"", Pop.Mia)]
    [InlineData("\"SG\"", Pop.Sg)]
    [InlineData("\"HK\"", Pop.Hk)]
    [InlineData("\"AMS\"", Pop.Ams)]
    public void TestPopWireValues(string json, Pop expected)
    {
        JsonConvert.DeserializeObject<Pop>(json).Should().Be(expected);
        JsonConvert.SerializeObject(expected).Should().Be(json);
    }
}
