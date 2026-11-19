using Didww.Api3.Http;

namespace Didww.Api3.Examples;

public static class CapacityPoolsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- List Capacity Pools ---");
        var queryParams = new QueryParams()
            .Include("shared_capacity_groups", "qty_based_pricings");

        var response = await client.CapacityPools().ListAsync(queryParams);
        Console.WriteLine($"  Capacity pools ({response.Data.Count}):");

        foreach (var pool in response.Data)
        {
            Console.WriteLine($"\n  {pool.Name}");
            Console.WriteLine($"    Total channels: {pool.TotalChannelsCount}");
            Console.WriteLine($"    Assigned channels: {pool.AssignedChannelsCount}");
            Console.WriteLine($"    Renew date: {pool.RenewDate}");

            if (pool.SharedCapacityGroups is { Count: > 0 })
            {
                Console.WriteLine($"    Shared capacity groups ({pool.SharedCapacityGroups.Count}):");
                foreach (var g in pool.SharedCapacityGroups)
                {
                    Console.WriteLine($"      {g.Name} shared={g.SharedChannelsCount} metered={g.MeteredChannelsCount}");
                }
            }

            if (pool.QtyBasedPricings is { Count: > 0 })
            {
                Console.WriteLine($"    Qty-based pricings ({pool.QtyBasedPricings.Count}):");
                foreach (var p in pool.QtyBasedPricings)
                {
                    Console.WriteLine($"      qty={p.Qty} setup={p.SetupPrice} monthly={p.MonthlyPrice}");
                }
            }
        }

        if (response.Data.Count > 0)
        {
            Console.WriteLine("\n--- Find Capacity Pool ---");
            var found = (await client.CapacityPools().FindAsync(response.Data[0].Id!, queryParams)).Data;
            Console.WriteLine($"  {found.Name} (ID: {found.Id})");
            Console.WriteLine($"    Minimum qty per order: {found.MinimumQtyPerOrder}, setup: {found.SetupPrice}, monthly: {found.MonthlyPrice}");
            Console.WriteLine($"    Shared capacity groups: {found.SharedCapacityGroups?.Count ?? 0}, qty-based pricings: {found.QtyBasedPricings?.Count ?? 0}");
        }
    }
}
