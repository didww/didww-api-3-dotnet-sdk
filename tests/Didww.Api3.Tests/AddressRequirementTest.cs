using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class AddressRequirementTest : BaseTest
{
    [Fact]
    public async Task TestListAddressRequirements()
    {
        StubGet("address_requirements", "address_requirements/index.json");

        var response = await Client.AddressRequirements().ListAsync();
        var requirements = response.Data;

        requirements.Should().NotBeEmpty();
        requirements[0].NumberType.Should().Be(NumberType.Local);
        requirements[0].PersonalMandatoryFields.Should().Equal("country");
        requirements[0].BusinessMandatoryFields.Should().BeEmpty();
        requirements[1].NumberType.Should().Be(NumberType.Mobile);
    }

    [Fact]
    public async Task TestFindAddressRequirement()
    {
        StubGet("address_requirements/25d12afe-1ec6-4fe3-9621-b250dd1fb959", "address_requirements/show.json");

        var response = await Client.AddressRequirements().FindAsync("25d12afe-1ec6-4fe3-9621-b250dd1fb959");
        var requirement = response.Data;

        requirement.Id.Should().Be("25d12afe-1ec6-4fe3-9621-b250dd1fb959");
        requirement.NumberType.Should().Be(NumberType.Local);
        requirement.PersonalMandatoryFields.Should().Equal("birth_date", "country", "personal_tax_id");
        requirement.BusinessMandatoryFields.Should().Equal("id_number", "tax_id", "country", "company_reg_number", "personal_tax_id");
        requirement.AddressProofTypes.Should().ContainSingle().Which.EntityType.Should().Be("address");
    }
}
