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

        var queryParams = new QueryParams().Include("trunk_group", "pop");
        var response = await Client.VoiceInTrunks().ListAsync(queryParams);
        var trunks = response.Data;

        trunks.Should().NotBeEmpty();

        var first = trunks[0];
        first.Id.Should().Be("2b4b1fcf-fe6a-4de9-8a58-7df46820ba13");
        first.Name.Should().Be("sample trunk pstn");
        first.Priority.Should().Be(1);
        first.Weight.Should().Be(65535);
        first.CliFormat.Should().Be(CliFormat.E164);
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
        config.Host.Should().Be("216.58.215.78");
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
            Host = "216.58.215.110",
            Port = 5060,
            CodecIds = new List<Codec> { Codec.PCMU, Codec.PCMA, Codec.G729, Codec.G723, Codec.TELEPHONE_EVENT },
            SstRefreshMethodId = SstRefreshMethod.INVITE,
            MediaEncryptionMode = MediaEncryptionMode.Zrtp,
            StirShakenMode = StirShakenMode.Pai,
            AllowedRtpIps = new List<string> { "127.0.0.1" }
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
            Host = "216.58.215.110",
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
            AllowedRtpIps = new List<string> { "127.0.0.1" }
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
}
