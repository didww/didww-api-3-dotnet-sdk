using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class CapacityPoolTest : BaseTest
{
    [Fact]
    public async Task TestListCapacityPools()
    {
        StubGet("capacity_pools", "capacity_pools/index.json");

        var response = await Client.CapacityPools().ListAsync();
        var pools = response.Data;

        pools.Should().NotBeEmpty();

        var first = pools[0];
        first.Id.Should().Be("f288d07c-e2fc-4ae6-9837-b18fb469c324");
        first.Name.Should().Be("Standard");
        first.TotalChannelsCount.Should().Be(34);
        first.AssignedChannelsCount.Should().Be(24);
    }

    [Fact]
    public async Task TestFindCapacityPool()
    {
        StubGet("capacity_pools/f288d07c-e2fc-4ae6-9837-b18fb469c324", "capacity_pools/show.json");

        var response = await Client.CapacityPools().FindAsync("f288d07c-e2fc-4ae6-9837-b18fb469c324");
        var pool = response.Data;

        pool.Id.Should().Be("f288d07c-e2fc-4ae6-9837-b18fb469c324");
        pool.Name.Should().Be("Standard");
    }

    [Fact]
    public async Task TestUpdateCapacityPool()
    {
        StubPatch("capacity_pools/f288d07c-e2fc-4ae6-9837-b18fb469c324", "capacity_pools/update_request.json", "capacity_pools/update.json");

        var pool = CapacityPool.Build("f288d07c-e2fc-4ae6-9837-b18fb469c324");
        pool.TotalChannelsCount = 25;

        var response = await Client.CapacityPools().UpdateAsync(pool);
        var updated = response.Data;

        updated.Id.Should().Be("f288d07c-e2fc-4ae6-9837-b18fb469c324");
        updated.Name.Should().Be("Standard");
        updated.TotalChannelsCount.Should().Be(25);
    }
}
