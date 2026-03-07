using Didww.Api3.Http;

namespace Didww.Api3.Examples;

public static class DidGroupsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- List DID Groups ---");
        var queryParams = new QueryParams()
            .Include("stock_keeping_units")
            .Page(1, 5);

        var response = await client.DidGroups().ListAsync(queryParams);
        Console.WriteLine($"  Found {response.Data.Count} DID groups");

        foreach (var group in response.Data)
        {
            var skuCount = group.StockKeepingUnits?.Count ?? 0;
            Console.WriteLine($"  {group.Id} - {group.AreaName} prefix={group.Prefix} features={string.Join(",", group.Features ?? [])} metered={group.IsMetered} ({skuCount} SKUs)");
        }

        if (response.Data.Count > 0)
        {
            Console.WriteLine("\n--- Find DID Group ---");
            var qp = new QueryParams().Include("stock_keeping_units");
            var found = await client.DidGroups().FindAsync(response.Data[0].Id!, qp);
            Console.WriteLine($"  Found: {found.Data.AreaName} prefix={found.Data.Prefix}");

            if (found.Data.StockKeepingUnits != null)
            {
                foreach (var sku in found.Data.StockKeepingUnits)
                {
                    Console.WriteLine($"    SKU: {sku.Id} setup={sku.SetupPrice} monthly={sku.MonthlyPrice} channels={sku.ChannelsIncludedCount}");
                }
            }
        }
    }
}
