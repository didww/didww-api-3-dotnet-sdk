using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class SenderIdVerificationTest : BaseTest
{
    private const string VerificationId = "e5a4e2f1-3b26-4a5c-9a0a-2f7c1f3e8d10";
    private const string HttpOutTrunkId = "1a2b3c4d-0003-4a5b-8c6d-7e8f9a0b1c03";

    [Fact]
    public async Task TestListSenderIdVerifications()
    {
        StubGet("sender_id_verifications", "sender_id_verifications/index.json");

        var response = await Client.SenderIdVerifications().ListAsync();
        var verifications = response.Data;

        verifications.Should().HaveCount(2);

        var active = verifications[0];
        active.Id.Should().Be(VerificationId);
        active.Name.Should().Be("ACME alerts");
        active.Status.Should().Be(SenderIdVerificationStatus.Active);
        active.IsActive.Should().BeTrue();
        active.SenderType.Should().Be(SenderType.LongCode);
        active.SenderIds.Should().Equal("12125550100", "12125550101");
        active.Reference.Should().Be("SID-1234");
        active.RejectReasons.Should().BeNull();
        active.RejectComment.Should().BeNull();
        active.RenewDate.Should().Be("2026-11-22");
        active.CreatedAt.Should().Be(new DateTimeOffset(2026, 10, 1, 10, 0, 0, TimeSpan.Zero));
        active.ActivatedAt.Should().Be(new DateTimeOffset(2026, 10, 3, 12, 30, 0, TimeSpan.Zero));
        active.ClosedAt.Should().BeNull();
        active.Meta!["setup_price"].ToString().Should().Be("10.0");
        active.Meta!["monthly_price"].ToString().Should().Be("15.0");

        var pending = verifications[1];
        pending.IsChangesRequired.Should().BeTrue();
        pending.IsActive.Should().BeFalse();
        pending.SenderType.Should().Be(SenderType.Alphanumeric);
        pending.RejectReasons.Should().Equal("Company documents are missing");
        pending.RejectComment.Should().Be("Upload the certificate of incorporation.");
        pending.RenewDate.Should().BeNull();
        pending.ActivatedAt.Should().BeNull();
    }

    [Fact]
    public async Task TestFindSenderIdVerificationWithIncludedSmsTrunk()
    {
        StubGet("sender_id_verifications/" + VerificationId, "sender_id_verifications/show_with_sms_trunk.json");

        var queryParams = new QueryParams().Include("sms_trunk");
        var response = await Client.SenderIdVerifications().FindAsync(VerificationId, queryParams);
        var verification = response.Data;

        verification.SmsTrunk.Should().NotBeNull();
        verification.SmsTrunk!.Id.Should().Be(HttpOutTrunkId);
        verification.SmsTrunk.Name.Should().Be("Outbound HTTP");
        verification.SmsTrunk.Configuration.Should().BeOfType<HttpOutConfiguration>();
    }

    [Fact]
    public async Task TestUpdateSenderIdVerificationName()
    {
        StubPatch("sender_id_verifications/" + VerificationId,
            "sender_id_verifications/update_name_request.json",
            "sender_id_verifications/update_name.json");

        var verification = SenderIdVerification.Build(VerificationId);
        verification.Name = "ACME notifications";

        var updated = (await Client.SenderIdVerifications().UpdateAsync(verification)).Data;

        updated.Name.Should().Be("ACME notifications");
    }

    [Fact]
    public async Task TestUpdateLoadedSenderIdVerificationSendsOnlyName()
    {
        StubGet("sender_id_verifications/" + VerificationId, "sender_id_verifications/show_with_sms_trunk.json");
        StubPatch("sender_id_verifications/" + VerificationId,
            "sender_id_verifications/update_name_request.json",
            "sender_id_verifications/update_name.json");

        var verification = (await Client.SenderIdVerifications().FindAsync(VerificationId)).Data;
        verification.Name = "ACME notifications";
        // Read-only attributes are never sent, even when changed locally.
        verification.Status = SenderIdVerificationStatus.Closed;
        verification.SenderIds = new List<string> { "12125550199" };

        await Client.SenderIdVerifications().UpdateAsync(verification);
    }

    [Fact]
    public async Task TestAssignSmsTrunk()
    {
        StubPatch("sender_id_verifications/" + VerificationId,
            "sender_id_verifications/assign_sms_trunk_request.json",
            "sender_id_verifications/assign_sms_trunk.json");

        var verification = SenderIdVerification.Build(VerificationId);
        verification.SmsTrunk = SmsTrunk.Build(HttpOutTrunkId);

        await Client.SenderIdVerifications().UpdateAsync(verification);
    }

    [Fact]
    public async Task TestUnassignSmsTrunkSendsNullData()
    {
        // Request body: "sms_trunk": {"data": null}.
        StubPatch("sender_id_verifications/" + VerificationId,
            "sender_id_verifications/unassign_sms_trunk_request.json",
            "sender_id_verifications/unassign_sms_trunk.json");

        var verification = SenderIdVerification.Build(VerificationId);
        verification.SmsTrunk = null;

        await Client.SenderIdVerifications().UpdateAsync(verification);
    }

    [Fact]
    public async Task TestUpdateClosedSenderIdVerification()
    {
        StubPatch("sender_id_verifications/" + VerificationId,
            "sender_id_verifications/update_closed_error.json", 422);

        var verification = SenderIdVerification.Build(VerificationId);
        verification.Name = "ACME notifications";

        var act = () => Client.SenderIdVerifications().UpdateAsync(verification);

        var ex = await act.Should().ThrowAsync<DidwwApiException>();
        ex.Which.HttpStatus.Should().Be(422);
        ex.Which.Errors.Should().HaveCount(1);
        ex.Which.Errors[0].Detail.Should().Be("Sender ID Verification is closed and can not be changed");
        ex.Which.Errors[0].Source!["pointer"].ToString().Should().Be("/data");
    }

    [Theory]
    [InlineData(SenderIdVerificationStatus.Active, true, false, false, false, false, false)]
    [InlineData(SenderIdVerificationStatus.ChangesRequired, false, true, false, false, false, false)]
    [InlineData(SenderIdVerificationStatus.Closed, false, false, true, false, false, false)]
    [InlineData(SenderIdVerificationStatus.ClosingInProcess, false, false, false, true, false, false)]
    [InlineData(SenderIdVerificationStatus.InProcess, false, false, false, false, true, false)]
    [InlineData(SenderIdVerificationStatus.New, false, false, false, false, false, true)]
    public void TestStatusHelpers(SenderIdVerificationStatus status, bool active, bool changesRequired,
        bool closed, bool closingInProcess, bool inProcess, bool isNew)
    {
        var verification = new SenderIdVerification { Status = status };

        verification.IsActive.Should().Be(active);
        verification.IsChangesRequired.Should().Be(changesRequired);
        verification.IsClosed.Should().Be(closed);
        verification.IsClosingInProcess.Should().Be(closingInProcess);
        verification.IsInProcess.Should().Be(inProcess);
        verification.IsNew.Should().Be(isNew);
    }
}
