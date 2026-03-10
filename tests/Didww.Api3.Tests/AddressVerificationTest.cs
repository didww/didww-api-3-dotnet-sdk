using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class AddressVerificationTest : BaseTest
{
    [Fact]
    public async Task TestListAddressVerifications()
    {
        StubGet("address_verifications", "address_verifications/index.json");

        var response = await Client.AddressVerifications().ListAsync();
        var verifications = response.Data;

        verifications.Should().NotBeEmpty();

        var first = verifications[0];
        first.Id.Should().Be("aaf2180a-3f2b-4427-888f-3d00f872014e");
        first.Status.Should().Be(AddressVerificationStatus.Pending);
    }

    [Fact]
    public async Task TestFindAddressVerification()
    {
        StubGet("address_verifications/c8e004b0-87ec-4987-b4fb-ee89db099f0e", "address_verifications/show.json");

        var response = await Client.AddressVerifications().FindAsync("c8e004b0-87ec-4987-b4fb-ee89db099f0e");
        var verification = response.Data;

        verification.Id.Should().Be("c8e004b0-87ec-4987-b4fb-ee89db099f0e");
        verification.Status.Should().Be(AddressVerificationStatus.Approved);
        verification.Reference.Should().Be("SHB-485120");
    }

    [Fact]
    public async Task TestFindRejectedAddressVerification()
    {
        StubGet("address_verifications/429e6d4e-2ee9-4953-aa98-0b3ac07f0f96", "address_verifications/show_rejected.json");

        var response = await Client.AddressVerifications().FindAsync("429e6d4e-2ee9-4953-aa98-0b3ac07f0f96");
        var verification = response.Data;

        verification.Id.Should().Be("429e6d4e-2ee9-4953-aa98-0b3ac07f0f96");
        verification.Status.Should().Be(AddressVerificationStatus.Rejected);
        verification.RejectReasons.Should().BeEquivalentTo(new[] { "Address cannot be validated", "Proof of address should be not older than of 6 months" });
        verification.Reference.Should().Be("ODW-879912");
    }

    [Fact]
    public async Task TestCreateAddressVerification()
    {
        StubPost("address_verifications", "address_verifications/create_request.json", "address_verifications/create.json");

        var verification = new AddressVerification
        {
            CallbackUrl = "http://example.com",
            CallbackMethod = CallbackMethod.Get,
            Address = Address.Build("d3414687-40f4-4346-a267-c2c65117d28c"),
            Dids = new List<Did> { Did.Build("a9d64c02-4486-4acb-a9a1-be4c81ff0659") }
        };

        var response = await Client.AddressVerifications().CreateAsync(verification);
        var created = response.Data;

        created.Id.Should().Be("78182ef2-8377-41cd-89e1-26e8266c9c94");
        created.Status.Should().Be(AddressVerificationStatus.Pending);
        created.CallbackUrl.Should().Be("http://example.com");
        created.CallbackMethod.Should().Be(CallbackMethod.Get);
    }
}
