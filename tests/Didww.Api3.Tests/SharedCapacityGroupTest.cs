using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class SharedCapacityGroupTest : BaseTest
{
    [Fact]
    public async Task TestListSharedCapacityGroups()
    {
        StubGet("shared_capacity_groups", "shared_capacity_groups/index.json");

        var response = await Client.SharedCapacityGroups().ListAsync();
        var groups = response.Data;

        groups.Should().NotBeEmpty();

        var first = groups[0];
        first.Id.Should().Be("89f987e2-0862-4bf4-a3f4-cdc89af0d875");
        first.Name.Should().Be("didww");
        first.SharedChannelsCount.Should().Be(19);
        first.MeteredChannelsCount.Should().Be(0);
    }

    [Fact]
    public async Task TestFindSharedCapacityGroup()
    {
        StubGet("shared_capacity_groups/89f987e2-0862-4bf4-a3f4-cdc89af0d875", "shared_capacity_groups/show.json");

        var response = await Client.SharedCapacityGroups().FindAsync("89f987e2-0862-4bf4-a3f4-cdc89af0d875");
        var group = response.Data;

        group.Id.Should().Be("89f987e2-0862-4bf4-a3f4-cdc89af0d875");
        group.Name.Should().Be("didww");
    }

    [Fact]
    public async Task TestCreateSharedCapacityGroup()
    {
        StubPost("shared_capacity_groups", "shared_capacity_groups/create_request.json", "shared_capacity_groups/create.json");

        var group = new SharedCapacityGroup
        {
            Name = "java-sdk",
            SharedChannelsCount = 5,
            MeteredChannelsCount = 0,
            CapacityPool = CapacityPool.Build("f288d07c-e2fc-4ae6-9837-b18fb469c324")
        };

        var response = await Client.SharedCapacityGroups().CreateAsync(group);
        var created = response.Data;

        created.Id.Should().Be("3688a9c3-354f-4e16-b458-1d2df9f02547");
        created.Name.Should().Be("java-sdk");
        created.SharedChannelsCount.Should().Be(5);
        created.MeteredChannelsCount.Should().Be(0);
    }

    [Fact]
    public async Task TestUpdateSharedCapacityGroup()
    {
        StubPatch("shared_capacity_groups/89f987e2-0862-4bf4-a3f4-cdc89af0d875", "shared_capacity_groups/update_request.json", "shared_capacity_groups/update.json");

        var group = SharedCapacityGroup.Build("89f987e2-0862-4bf4-a3f4-cdc89af0d875");
        group.Name = "didww1";
        group.SharedChannelsCount = 10;
        group.MeteredChannelsCount = 2;

        var response = await Client.SharedCapacityGroups().UpdateAsync(group);
        var updated = response.Data;

        updated.Id.Should().Be("89f987e2-0862-4bf4-a3f4-cdc89af0d875");
        updated.Name.Should().Be("didww1");
        updated.SharedChannelsCount.Should().Be(10);
        updated.MeteredChannelsCount.Should().Be(2);
    }

    [Fact]
    public async Task TestDeleteSharedCapacityGroup()
    {
        var id = "89f987e2-0862-4bf4-a3f4-cdc89af0d875";
        StubDelete("shared_capacity_groups/" + id);

        await Client.SharedCapacityGroups().DeleteAsync(id);
    }
}
