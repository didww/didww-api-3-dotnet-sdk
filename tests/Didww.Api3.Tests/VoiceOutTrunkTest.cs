using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration.AuthenticationMethod;
using Didww.Api3.Resource.Enums;
using FluentAssertions;
using Newtonsoft.Json;

namespace Didww.Api3.Tests;

public class VoiceOutTrunkTest : BaseTest
{
    [Fact]
    public async Task TestListVoiceOutTrunks()
    {
        StubGet("voice_out_trunks", "voice_out_trunks/index.json");

        var response = await Client.VoiceOutTrunks().ListAsync();
        var trunks = response.Data;

        trunks.Should().NotBeEmpty();

        var first = trunks[0];
        first.Id.Should().Be("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        first.Name.Should().Be("test");
        first.Status.Should().Be(VoiceOutTrunkStatus.Blocked);
        first.OnCliMismatchAction.Should().Be(OnCliMismatchAction.ReplaceCli);
        first.CapacityLimit.Should().Be(123);
        first.AllowAnyDidAsCli.Should().BeFalse();
        first.MediaEncryptionMode.Should().Be(MediaEncryptionMode.SrtpSdes);
        first.DefaultDstAction.Should().Be(DefaultDstAction.RejectAll);
        first.ForceSymmetricRtp.Should().BeTrue();
        first.RtpPing.Should().BeTrue();
        first.ThresholdReached.Should().BeFalse();
        first.ThresholdAmount.Should().Be(200.0m);
        first.DstPrefixes.Should().ContainSingle().Which.Should().Be("370");
        first.Codecs.Should().Equal(VoiceOutCodec.Opus, VoiceOutCodec.G722, VoiceOutCodec.Pcma, VoiceOutCodec.TelephoneEvent);
        first.IceMode.Should().Be(IceMode.Initiate);
        first.RtcpMuxMode.Should().Be(RtcpMuxMode.Disabled);
        first.RtcpFeedbackMode.Should().Be(RtcpFeedbackMode.Initiate);

        var second = trunks[1];
        second.Codecs.Should().Equal(VoiceOutCodec.Pcma, VoiceOutCodec.Pcmu, VoiceOutCodec.TelephoneEvent);
        second.IceMode.Should().Be(IceMode.Accept);
        second.RtcpMuxMode.Should().Be(RtcpMuxMode.Accept);
        second.RtcpFeedbackMode.Should().Be(RtcpFeedbackMode.Accept);

        first.AuthenticationMethod.Should().BeOfType<CredentialsAndIpAuthenticationMethod>();
        var cai = (CredentialsAndIpAuthenticationMethod)first.AuthenticationMethod!;
        cai.AllowedSipIps.Should().ContainSingle().Which.Should().Be("203.0.113.1/32");
        cai.Username.Should().Be("dpjgwbbac9");
        cai.Password.Should().Be("z0hshvbcy7");
    }

    [Fact]
    public async Task TestFindVoiceOutTrunk()
    {
        StubGet("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864", "voice_out_trunks/show.json");

        var response = await Client.VoiceOutTrunks().FindAsync("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        var trunk = response.Data;

        trunk.Id.Should().Be("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.Name.Should().Be("test");
        trunk.ExternalReferenceId.Should().Be("crm-vot-0001");
        trunk.EmergencyEnableAll.Should().BeFalse();
        trunk.RtpTimeout.Should().Be(30);
        trunk.Codecs.Should().Equal(VoiceOutCodec.AmrWb, VoiceOutCodec.Pcmu);
        trunk.IceMode.Should().Be(IceMode.Disabled);
        trunk.RtcpMuxMode.Should().Be(RtcpMuxMode.Initiate);
        trunk.RtcpFeedbackMode.Should().Be(RtcpFeedbackMode.Disabled);
        trunk.Dids.Should().HaveCount(2);
        trunk.DefaultDid.Should().NotBeNull();
        trunk.DefaultDid!.Number.Should().Be("37061498222");

        trunk.AuthenticationMethod.Should().BeOfType<CredentialsAndIpAuthenticationMethod>();
        var cai = (CredentialsAndIpAuthenticationMethod)trunk.AuthenticationMethod!;
        cai.AllowedSipIps.Should().ContainSingle().Which.Should().Be("203.0.113.1/32");
        cai.Username.Should().Be("dpjgwbbac9");
        cai.Password.Should().Be("z0hshvbcy7");
    }

    [Fact]
    public async Task TestFindVoiceOutTrunkWithIpOnlyAuth()
    {
        StubGet("voice_out_trunks/23fd58f9-9094-406c-bfd9-f4d25bda13c6", "voice_out_trunks/show_ip_only.json");

        var response = await Client.VoiceOutTrunks().FindAsync("23fd58f9-9094-406c-bfd9-f4d25bda13c6");
        var trunk = response.Data;

        trunk.Id.Should().Be("23fd58f9-9094-406c-bfd9-f4d25bda13c6");
        trunk.Name.Should().Be("SDK Test credentials_and_ip");
        trunk.Status.Should().Be(VoiceOutTrunkStatus.Active);

        // authentication_method must be IpOnly, not CredentialsAndIp
        trunk.AuthenticationMethod.Should().BeOfType<IpOnlyAuthenticationMethod>();
        trunk.AuthenticationMethod.Should().NotBeOfType<CredentialsAndIpAuthenticationMethod>();

        var ipOnly = (IpOnlyAuthenticationMethod)trunk.AuthenticationMethod!;
        ipOnly.AllowedSipIps.Should().ContainSingle().Which.Should().Be("203.0.113.1/32");
    }

    [Fact]
    public async Task TestFindVoiceOutTrunkWithTwilioAuth()
    {
        StubGet("voice_out_trunks/b5e701f4-ea15-4f9d-8f35-6a0bdce04385", "voice_out_trunks/show_twilio.json");

        var response = await Client.VoiceOutTrunks().FindAsync("b5e701f4-ea15-4f9d-8f35-6a0bdce04385");
        var trunk = response.Data;

        trunk.Id.Should().Be("b5e701f4-ea15-4f9d-8f35-6a0bdce04385");
        trunk.Name.Should().Be("SDK Test twilio");
        trunk.Status.Should().Be(VoiceOutTrunkStatus.Active);

        // authentication_method must be Twilio
        trunk.AuthenticationMethod.Should().BeOfType<TwilioAuthenticationMethod>();

        var twilio = (TwilioAuthenticationMethod)trunk.AuthenticationMethod!;
        twilio.TwilioAccountSid.Should().Be("AC22222222222222222222222222222222");
    }

    [Fact]
    public async Task TestCreateVoiceOutTrunkWithTwilioAuth()
    {
        StubPost("voice_out_trunks",
            "voice_out_trunks/create_twilio_request.json", "voice_out_trunks/create_twilio.json");

        var trunk = new VoiceOutTrunk
        {
            Name = "SDK Test twilio create",
            OnCliMismatchAction = OnCliMismatchAction.RejectCall,
            AuthenticationMethod = new TwilioAuthenticationMethod
            {
                TwilioAccountSid = "AC33333333333333333333333333333333"
            }
        };

        var response = await Client.VoiceOutTrunks().CreateAsync(trunk);
        var created = response.Data;

        created.Id.Should().Be("507fa5a2-fd58-4c4d-a231-efba27f67c3a");
        created.Name.Should().Be("SDK Test twilio create");
        created.Status.Should().Be(VoiceOutTrunkStatus.Active);

        created.AuthenticationMethod.Should().BeOfType<TwilioAuthenticationMethod>();
        var twilio = (TwilioAuthenticationMethod)created.AuthenticationMethod!;
        twilio.TwilioAccountSid.Should().Be("AC33333333333333333333333333333333");
    }

    [Fact]
    public async Task TestCreateVoiceOutTrunkWithIpOnlyAuthenticationMethod()
    {
        StubPost("voice_out_trunks",
            "voice_out_trunks/create_request.json", "voice_out_trunks/create.json");

        var did = Did.Build("7a028c32-e6b6-4c86-bf01-90f901b37012");
        var trunk = new VoiceOutTrunk
        {
            Name = "java-test",
            OnCliMismatchAction = OnCliMismatchAction.ReplaceCli,
            AuthenticationMethod = new IpOnlyAuthenticationMethod
            {
                AllowedSipIps = new List<string> { "203.0.113.0/24" },
                TechPrefix = ""
            },
            DefaultDid = did,
            Dids = new List<Did> { did },
            Codecs = new List<VoiceOutCodec> { VoiceOutCodec.Opus, VoiceOutCodec.Pcma, VoiceOutCodec.TelephoneEvent },
            IceMode = IceMode.Initiate,
            RtcpMuxMode = RtcpMuxMode.Disabled,
            RtcpFeedbackMode = RtcpFeedbackMode.Accept
        };

        var response = await Client.VoiceOutTrunks().CreateAsync(trunk);
        var created = response.Data;

        created.Id.Should().Be("b60201c1-21f0-4d9a-aafa-0e6d1e12f22e");
        created.Name.Should().Be("java-test");
        created.Status.Should().Be(VoiceOutTrunkStatus.Active);

        created.AuthenticationMethod.Should().BeOfType<CredentialsAndIpAuthenticationMethod>();
        var cai = (CredentialsAndIpAuthenticationMethod)created.AuthenticationMethod!;
        cai.Username.Should().NotBeNullOrEmpty();
        cai.Password.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkSendsOnlyDirtyFields()
    {
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_request.json", "voice_out_trunks/update.json");

        var trunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.Name = "test";
        trunk.CapacityLimit = 123;
        trunk.OnCliMismatchAction = OnCliMismatchAction.ReplaceCli;
        trunk.DefaultDstAction = DefaultDstAction.RejectAll;
        trunk.DstPrefixes = new List<string> { "370" };
        trunk.ForceSymmetricRtp = true;
        trunk.RtpPing = true;
        trunk.Codecs = new List<VoiceOutCodec> { VoiceOutCodec.G722, VoiceOutCodec.Amr, VoiceOutCodec.Gsm };
        trunk.IceMode = IceMode.Disabled;
        trunk.RtcpMuxMode = RtcpMuxMode.Initiate;
        trunk.RtcpFeedbackMode = RtcpFeedbackMode.Disabled;
        trunk.AuthenticationMethod = new IpOnlyAuthenticationMethod
        {
            AllowedSipIps = new List<string> { "203.0.113.1/32" },
            TechPrefix = ""
        };

        var response = await Client.VoiceOutTrunks().UpdateAsync(trunk);
        var updated = response.Data;

        updated.Id.Should().Be("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        updated.Name.Should().Be("test");
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkFromLoadedResource()
    {
        StubGet("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864", "voice_out_trunks/show.json");
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_from_loaded_request.json", "voice_out_trunks/update.json");

        var trunk = (await Client.VoiceOutTrunks().FindAsync("425ce763-a3a9-49b4-af5b-ada1a65c8864")).Data;
        trunk.CallbackUrl = "https://example.com/callback";
        trunk.AllowAnyDidAsCli = true;
        trunk.ThresholdAmount = 500.0m;

        await Client.VoiceOutTrunks().UpdateAsync(trunk);
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkReassignAuthenticationMethod()
    {
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_authentication_method_request.json",
            "voice_out_trunks/update.json");

        var trunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.AuthenticationMethod = new CredentialsAndIpAuthenticationMethod
        {
            AllowedSipIps = new List<string> { "192.0.2.10/32" },
            TechPrefix = "99"
        };

        await Client.VoiceOutTrunks().UpdateAsync(trunk);
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkToggleEmergencyEnableAll()
    {
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_emergency_enable_all_request.json",
            "voice_out_trunks/update.json");

        var trunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.EmergencyEnableAll = true;

        await Client.VoiceOutTrunks().UpdateAsync(trunk);
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkReplaceEmergencyDids()
    {
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_emergency_dids_request.json",
            "voice_out_trunks/update_emergency_dids.json");

        var trunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.EmergencyDids = new List<Did>
        {
            Did.Build("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Did.Build("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")
        };

        await Client.VoiceOutTrunks().UpdateAsync(trunk);
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkClearEmergencyDids()
    {
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_clear_emergency_dids_request.json",
            "voice_out_trunks/update_emergency_dids.json");

        var trunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.EmergencyDids = new List<Did>();

        await Client.VoiceOutTrunks().UpdateAsync(trunk);
    }

    [Fact]
    public async Task TestUpdateVoiceOutTrunkExternalReferenceId()
    {
        StubPatch("voice_out_trunks/425ce763-a3a9-49b4-af5b-ada1a65c8864",
            "voice_out_trunks/update_external_reference_id_request.json",
            "voice_out_trunks/update.json");

        var trunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        trunk.ExternalReferenceId = "crm-vot-0002";

        await Client.VoiceOutTrunks().UpdateAsync(trunk);
    }

    [Theory]
    [InlineData("\"telephone_event\"", VoiceOutCodec.TelephoneEvent)]
    [InlineData("\"g723\"", VoiceOutCodec.G723)]
    [InlineData("\"g729\"", VoiceOutCodec.G729)]
    [InlineData("\"pcmu\"", VoiceOutCodec.Pcmu)]
    [InlineData("\"pcma\"", VoiceOutCodec.Pcma)]
    [InlineData("\"gsm\"", VoiceOutCodec.Gsm)]
    [InlineData("\"g722\"", VoiceOutCodec.G722)]
    [InlineData("\"opus\"", VoiceOutCodec.Opus)]
    [InlineData("\"amr\"", VoiceOutCodec.Amr)]
    [InlineData("\"amr_wb\"", VoiceOutCodec.AmrWb)]
    public void TestVoiceOutCodecWireValues(string json, VoiceOutCodec expected)
    {
        JsonConvert.DeserializeObject<VoiceOutCodec>(json).Should().Be(expected);
        JsonConvert.SerializeObject(expected).Should().Be(json);
    }

    [Fact]
    public void TestVoiceOutCodecHasOneMemberPerDocumentedName()
    {
        Enum.GetValues<VoiceOutCodec>().Select(c => JsonConvert.SerializeObject(c).Trim('"')).Should().Equal(
            "telephone_event", "g723", "g729", "pcmu", "pcma", "gsm", "g722", "opus", "amr", "amr_wb");
    }

    [Fact]
    public void TestStatusHelperActive()
    {
        var trunk = new VoiceOutTrunk { Status = VoiceOutTrunkStatus.Active };
        trunk.IsActive.Should().BeTrue();
        trunk.IsBlocked.Should().BeFalse();
    }

    [Fact]
    public void TestStatusHelperBlocked()
    {
        var trunk = new VoiceOutTrunk { Status = VoiceOutTrunkStatus.Blocked };
        trunk.IsBlocked.Should().BeTrue();
        trunk.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task TestDeleteVoiceOutTrunk()
    {
        var id = "425ce763-a3a9-49b4-af5b-ada1a65c8864";
        StubDelete("voice_out_trunks/" + id);

        await Client.VoiceOutTrunks().DeleteAsync(id);
    }

    [Fact]
    public async Task TestCreateVoiceOutTrunkRegenerateCredential()
    {
        StubPost("voice_out_trunk_regenerate_credentials",
            "voice_out_trunk_regenerate_credentials/create.json");

        var regen = VoiceOutTrunkRegenerateCredential.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");
        regen.VoiceOutTrunk = VoiceOutTrunk.Build("425ce763-a3a9-49b4-af5b-ada1a65c8864");

        var response = await Client.VoiceOutTrunkRegenerateCredentials().CreateAsync(regen);
        response.Data.Id.Should().Be("5fc59e7e-79eb-498a-8779-800416b5c68a");
    }
}
