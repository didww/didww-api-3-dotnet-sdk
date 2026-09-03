using FluentAssertions;

namespace Didww.Api3.Tests;

public class EmergencyRequirementTest : BaseTest
{
    [Fact]
    public async Task TestListEmergencyRequirements()
    {
        StubGet("emergency_requirements", "emergency_requirements/index.json");

        var response = await Client.EmergencyRequirements().ListAsync();
        var requirements = response.Data;

        requirements.Should().NotBeEmpty();
        requirements.Should().HaveCount(1);

        var first = requirements[0];
        first.Id.Should().Be("11111111-2222-3333-4444-555555555555");
        first.IdentityType.Should().Be("personal");
        first.AddressAreaLevel.Should().Be("city");
        first.PersonalAreaLevel.Should().Be("country");
        first.BusinessAreaLevel.Should().BeNull();
        first.AddressMandatoryFields.Should().BeEquivalentTo(new[] { "street", "city", "postal_code" });
        first.PersonalMandatoryFields.Should().BeEquivalentTo(new[] { "first_name", "last_name" });
        first.BusinessMandatoryFields.Should().BeEmpty();
        first.EstimateSetupTime.Should().Be("7-14 days");
        first.RequirementRestrictionMessage.Should().BeNull();
        first.Meta.Should().NotBeNull();
        first.Meta!["setup_price"]!.ToString().Should().Be("0.0");
        first.Meta!["monthly_price"]!.ToString().Should().Be("0.0");
    }

    [Fact]
    public async Task TestFindEmergencyRequirement()
    {
        StubGet("emergency_requirements/01234567-89ab-cdef-0123-456789abcdef",
            "emergency_requirements/show.json");

        var response = await Client.EmergencyRequirements()
            .FindAsync("01234567-89ab-cdef-0123-456789abcdef");
        var requirement = response.Data;

        requirement.Id.Should().Be("01234567-89ab-cdef-0123-456789abcdef");
        requirement.IdentityType.Should().Be("business");
        requirement.AddressAreaLevel.Should().Be("area");
        requirement.PersonalAreaLevel.Should().BeNull();
        requirement.BusinessAreaLevel.Should().Be("world_wide");
        requirement.EstimateSetupTime.Should().Be("7-14 days");
        requirement.RequirementRestrictionMessage.Should()
            .Be("Additional compliance review is required for this country.");
        requirement.Meta.Should().NotBeNull();
        requirement.Meta!["setup_price"]!.ToString().Should().Be("10.0");
        requirement.Meta!["monthly_price"]!.ToString().Should().Be("2.5");
    }
}
