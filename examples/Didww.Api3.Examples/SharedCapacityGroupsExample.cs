using Didww.Api3.Http;
using Didww.Api3.Resource;

namespace Didww.Api3.Examples;

public static class SharedCapacityGroupsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- List Capacity Pools ---");
        var pools = await client.CapacityPools().ListAsync(new QueryParams().Page(1, 1));
        if (pools.Data.Count == 0)
        {
            Console.WriteLine("  No capacity pools found, skipping.");
            return;
        }

        var pool = pools.Data[0];
        Console.WriteLine($"  Using pool: {pool.Name} (ID: {pool.Id})");

        Console.WriteLine("\n--- Create Shared Capacity Group ---");
        var suffix = Guid.NewGuid().ToString()[..8];
        var group = new SharedCapacityGroup
        {
            Name = "SDK Example Group " + suffix,
            SharedChannelsCount = pool.MinimumLimit ?? 1,
            CapacityPool = CapacityPool.Build(pool.Id!)
        };

        var response = await client.SharedCapacityGroups().CreateAsync(group);
        var created = response.Data;
        Console.WriteLine($"  Created: {created.Id}");
        Console.WriteLine($"    Name: {created.Name}");
        Console.WriteLine($"    Shared channels: {created.SharedChannelsCount}");
        Console.WriteLine($"    Metered channels: {created.MeteredChannelsCount}");

        Console.WriteLine("\n--- List Shared Capacity Groups ---");
        var listResponse = await client.SharedCapacityGroups().ListAsync();
        foreach (var g in listResponse.Data)
        {
            Console.WriteLine($"  {g.Name} (shared: {g.SharedChannelsCount}, metered: {g.MeteredChannelsCount})");
        }

        Console.WriteLine("\n--- Update Shared Capacity Group ---");
        created.Name = "Updated Group " + suffix;
        var updated = (await client.SharedCapacityGroups().UpdateAsync(created)).Data;
        Console.WriteLine($"  Updated name: {updated.Name}");

        Console.WriteLine("\n--- Delete Shared Capacity Group ---");
        await client.SharedCapacityGroups().DeleteAsync(created.Id!);
        Console.WriteLine($"  Deleted: {created.Id}");
    }
}
