using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class IdentityTest : BaseTest
{
    [Fact]
    public async Task TestListIdentities()
    {
        StubGet("identities", "identities/index.json");

        var response = await Client.Identities().ListAsync();
        var identities = response.Data;

        identities.Should().NotBeEmpty();

        var first = identities[0];
        first.Id.Should().Be("5e9df058-50d2-4e34-b0d4-d1746b86f41a");
        first.FirstName.Should().Be("John");
        first.LastName.Should().Be("Doe");
        first.IdentityType.Should().Be(IdentityType.Personal);
    }

    [Fact]
    public async Task TestCreateIdentity()
    {
        StubPost("identities", "identities/create.json");

        var identity = new Identity
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123456789",
            IdNumber = "ABC1234",
            BirthDate = "1970-01-01",
            CompanyName = "Test Company Limited",
            CompanyRegNumber = "543221",
            VatId = "GB1234",
            Description = "test identity",
            PersonalTaxId = "987654321",
            IdentityType = IdentityType.Business,
            ExternalReferenceId = "111",
            Country = Country.Build("1f6fc2bd-f081-4202-9b1a-d9cb88d942b9")
        };

        var response = await Client.Identities().CreateAsync(identity);
        var created = response.Data;

        created.Id.Should().Be("e96ae7d1-11d5-42bc-a5c5-211f3c3788ae");
        created.FirstName.Should().Be("John");
        created.LastName.Should().Be("Doe");
        created.IdentityType.Should().Be(IdentityType.Business);
        created.CompanyName.Should().Be("Test Company Limited");
        created.ExternalReferenceId.Should().Be("111");
        created.Verified.Should().BeFalse();
    }

    [Fact]
    public async Task TestCreatePersonalIdentity()
    {
        StubPost("identities", "identities/create_personal.json");

        var identity = new Identity
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "12345678",
            IdentityType = IdentityType.Personal,
            Country = Country.Build("some-country-id")
        };

        var response = await Client.Identities().CreateAsync(identity);
        var created = response.Data;

        created.Id.Should().Be("9728ea13-cb5d-41fb-8a7f-796a005b0a13");
        created.IdentityType.Should().Be(IdentityType.Personal);
    }

    [Fact]
    public async Task TestUpdateIdentity()
    {
        StubPatch("identities/e96ae7d1-11d5-42bc-a5c5-211f3c3788ae", "identities/update.json");

        var identity = Identity.Build("e96ae7d1-11d5-42bc-a5c5-211f3c3788ae");
        identity.FirstName = "Jake";
        identity.LastName = "Johnson";
        identity.Description = "test";

        var response = await Client.Identities().UpdateAsync(identity);
        var updated = response.Data;

        updated.Id.Should().Be("e96ae7d1-11d5-42bc-a5c5-211f3c3788ae");
        updated.FirstName.Should().Be("Jake");
        updated.LastName.Should().Be("Johnson");
        updated.Description.Should().Be("test");
    }

    [Fact]
    public async Task TestDeleteIdentity()
    {
        var id = "e96ae7d1-11d5-42bc-a5c5-211f3c3788ae";
        StubDelete("identities/" + id);

        await Client.Identities().DeleteAsync(id);
    }
}
