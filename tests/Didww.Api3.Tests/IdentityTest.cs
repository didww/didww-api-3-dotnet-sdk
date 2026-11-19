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
        StubPost("identities", "identities/create_request.json", "identities/create.json");

        var identity = new Identity
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "123456789",
            IdNumber = "ABC1234",
            BirthDate = new DateOnly(1970, 1, 1),
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
        created.BirthCountry.Should().NotBeNull();
        created.BirthCountry!.Id.Should().Be("1f6fc2bd-f081-4202-9b1a-d9cb88d942b9");
    }

    [Fact]
    public async Task TestFindIdentityWithBirthCountry()
    {
        StubGet("identities/e96ae7d1-11d5-42bc-a5c5-211f3c3788ae", "identities/show.json");

        var response = await Client.Identities().FindAsync("e96ae7d1-11d5-42bc-a5c5-211f3c3788ae");
        var identity = response.Data;

        identity.Id.Should().Be("e96ae7d1-11d5-42bc-a5c5-211f3c3788ae");
        identity.FirstName.Should().Be("John");
        identity.IdentityType.Should().Be(IdentityType.Business);

        // country and birth_country should resolve to different Country objects
        identity.Country.Should().NotBeNull();
        identity.Country!.Id.Should().Be("1f6fc2bd-f081-4202-9b1a-d9cb88d942b9");
        identity.Country!.Name.Should().Be("United States");

        identity.BirthCountry.Should().NotBeNull();
        identity.BirthCountry!.Id.Should().Be("a1b2c3d4-e5f6-7890-abcd-ef1234567890");
        identity.BirthCountry!.Name.Should().Be("Germany");
        identity.BirthCountry!.Iso.Should().Be("DE");
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
        StubPatch("identities/e96ae7d1-11d5-42bc-a5c5-211f3c3788ae", "identities/update_request.json", "identities/update.json");

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

    private static Identity NewBusinessIdentity() => new()
    {
        FirstName = "John",
        LastName = "Doe",
        PhoneNumber = "123456789",
        IdNumber = "ABC1234",
        BirthDate = new DateOnly(1970, 1, 1),
        CompanyName = "Test Company Limited",
        CompanyRegNumber = "543221",
        VatId = "GB1234",
        Description = "test identity",
        PersonalTaxId = "987654321",
        IdentityType = IdentityType.Business,
        ExternalReferenceId = "111",
        Country = Country.Build("1f6fc2bd-f081-4202-9b1a-d9cb88d942b9")
    };

    [Fact]
    public async Task TestCreateIdentityOmitsNullCompanyWebsite()
    {
        // The request fixture has no company_website key: a null value is left out.
        StubPost("identities", "identities/create_request.json", "identities/create.json");

        var identity = NewBusinessIdentity();
        identity.CompanyWebsite.Should().BeNull();

        await Client.Identities().CreateAsync(identity);

        WireMock.LogEntries
            .Single(e => e.RequestMessage.Method == "POST" && e.RequestMessage.Path == "/v3/identities")
            .RequestMessage.Body.Should().NotContain("company_website");
    }

    [Fact]
    public async Task TestCreateIdentityWithCompanyWebsite()
    {
        StubPost("identities", "identities/create_with_company_website_request.json", "identities/create.json");

        var identity = NewBusinessIdentity();
        identity.CompanyWebsite = "https://www.example.org";

        var response = await Client.Identities().CreateAsync(identity);

        response.Data.Id.Should().Be("e96ae7d1-11d5-42bc-a5c5-211f3c3788ae");
    }

    [Fact]
    public async Task TestUpdateIdentityCompanyWebsite()
    {
        StubPatch("identities/e96ae7d1-11d5-42bc-a5c5-211f3c3788ae",
            "identities/update_company_website_request.json", "identities/update_company_website.json");

        var identity = Identity.Build("e96ae7d1-11d5-42bc-a5c5-211f3c3788ae");
        identity.CompanyWebsite = "https://www.example.org";

        var updated = (await Client.Identities().UpdateAsync(identity)).Data;

        updated.CompanyWebsite.Should().Be("https://www.example.org");
    }
}
