using Didww.Api3.Http;
using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class DidGroupTest : BaseTest
{
    [Fact]
    public void TestFeatureEnumDoesNotIncludeSmsOut()
    {
        var featureNames = Enum.GetNames(typeof(Feature));
        featureNames.Should().NotContain("SmsOut");
    }

    [Fact]
    public void TestFeatureEnumIncludesNewFeatures()
    {
        var featureNames = Enum.GetNames(typeof(Feature));
        featureNames.Should().Contain("P2p");
        featureNames.Should().Contain("A2p");
        featureNames.Should().Contain("Emergency");
        featureNames.Should().Contain("CnamOut");
    }

    [Fact]
    public async Task TestListDidGroups()
    {
        StubGet("did_groups", "did_groups/index.json");

        var response = await Client.DidGroups().ListAsync();
        var groups = response.Data;

        groups.Should().NotBeEmpty();

        var first = groups[0];
        first.Id.Should().Be("f6cc4ea6-6dda-4c51-9952-c1440441bc2f");
        first.AreaName.Should().Be("Inman");
        first.Prefix.Should().Be("864");
    }

    [Fact]
    public async Task TestFindDidGroup()
    {
        StubGet("did_groups/2187c36d-28fb-436f-8861-5a0f5b5a3ee1", "did_groups/show.json");

        var queryParams = new QueryParams().Include("country");
        var response = await Client.DidGroups().FindAsync("2187c36d-28fb-436f-8861-5a0f5b5a3ee1", queryParams);
        var group = response.Data;

        group.Id.Should().Be("2187c36d-28fb-436f-8861-5a0f5b5a3ee1");
        group.AreaName.Should().Be("Aachen");
        group.Prefix.Should().Be("241");
        group.NumberType.Should().Be(NumberType.Local);
        group.IsDemo.Should().BeFalse();
    }

    [Fact]
    public async Task TestFindDidGroupWithAddressRequirement()
    {
        StubGet("did_groups/2187c36d-28fb-436f-8861-5a0f5b5a3ee1", "did_groups/show_with_address_requirement.json");

        var queryParams = new QueryParams().Include("address_requirement");
        var response = await Client.DidGroups().FindAsync("2187c36d-28fb-436f-8861-5a0f5b5a3ee1", queryParams);
        var group = response.Data;

        group.Id.Should().Be("2187c36d-28fb-436f-8861-5a0f5b5a3ee1");
        group.AreaName.Should().Be("Aachen");
        group.Prefix.Should().Be("241");
        group.AddressRequirement.Should().NotBeNull();
        group.AddressRequirement!.Id.Should().Be("8da1e0b2-047c-4baf-9c57-57143f09b9ce");
        group.AddressRequirement!.IdentityType.Should().Be(IdentityType.Any);
    }

    [Fact]
    public async Task TestFindDidGroupServiceRestrictionsNull()
    {
        StubGet("did_groups/2187c36d-28fb-436f-8861-5a0f5b5a3ee1", "did_groups/show.json");

        var response = await Client.DidGroups().FindAsync("2187c36d-28fb-436f-8861-5a0f5b5a3ee1");
        var group = response.Data;

        group.ServiceRestrictions.Should().BeNull();
    }

    [Fact]
    public async Task TestFindDidGroupServiceRestrictionsPresent()
    {
        StubGet("did_groups/aaa11111-bbbb-cccc-dddd-eeeeeeeeeeee",
            "did_groups/show_with_service_restrictions.json");

        var response = await Client.DidGroups().FindAsync("aaa11111-bbbb-cccc-dddd-eeeeeeeeeeee");
        var group = response.Data;

        group.ServiceRestrictions.Should().Be("Emergency services only. Voice-out not available.");
        group.AllowAdditionalChannels.Should().BeFalse();
        group.Features.Should().Contain(Feature.Emergency);
        group.NumberType.Should().Be(NumberType.Global);
        group.IsDemo.Should().BeTrue();
    }

    [Theory]
    [InlineData("\"local\"", NumberType.Local)]
    [InlineData("\"national\"", NumberType.National)]
    [InlineData("\"mobile\"", NumberType.Mobile)]
    [InlineData("\"toll_free\"", NumberType.TollFree)]
    [InlineData("\"shared_cost\"", NumberType.SharedCost)]
    [InlineData("\"global\"", NumberType.Global)]
    public void TestNumberTypeWireValues(string json, NumberType expected)
    {
        Newtonsoft.Json.JsonConvert.DeserializeObject<NumberType>(json).Should().Be(expected);
        Newtonsoft.Json.JsonConvert.SerializeObject(expected).Should().Be(json);
    }

    [Fact]
    public async Task TestFindDidGroupWithStockKeepingUnits()
    {
        StubGet("did_groups/2187c36d-28fb-436f-8861-5a0f5b5a3ee1",
            "did_groups/show.json");

        var queryParams = new QueryParams().Include("stock_keeping_units", "country");
        var response = await Client.DidGroups()
            .FindAsync("2187c36d-28fb-436f-8861-5a0f5b5a3ee1", queryParams);
        var group = response.Data;

        group.Id.Should().Be("2187c36d-28fb-436f-8861-5a0f5b5a3ee1");
        group.Prefix.Should().Be("241");
        group.StockKeepingUnits.Should().NotBeNullOrEmpty();
        group.StockKeepingUnits.Should().HaveCount(2);

        var sku = group.StockKeepingUnits![0];
        sku.Id.Should().Be("5c6f00cd-cfca-441f-9322-5d000458b44f");
        sku.ChannelsIncludedCount.Should().Be(0);
    }
}
