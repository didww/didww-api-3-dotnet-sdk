using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

public static class OrdersNanpaExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Order DID by NPA/NXX prefix ---");

        // Step 1: find the NANPA prefix by NPA/NXX (NPA+NXX without dash, e.g. 201221 for NPA=201, NXX=221)
        const string npanxx = "201221";
        var nanpaParams = new QueryParams()
            .Filter("npanxx", npanxx)
            .Page(1, 1);
        var nanpaPrefixResponse = await client.NanpaPrefixes().ListAsync(nanpaParams);
        if (nanpaPrefixResponse.Data.Count == 0)
        {
            Console.WriteLine($"  NANPA prefix {npanxx} not found, skipping.");
            return;
        }

        var nanpaPrefix = nanpaPrefixResponse.Data[0];
        Console.WriteLine($"  NANPA prefix: {nanpaPrefix.Id} NPA={nanpaPrefix.Npa} NXX={nanpaPrefix.Nxx}");

        // Step 2: find a DID group for this prefix and load its SKUs
        var dgParams = new QueryParams()
            .Filter("nanpa_prefix.id", nanpaPrefix.Id!)
            .Include("stock_keeping_units")
            .Page(1, 1);
        var didGroups = await client.DidGroups().ListAsync(dgParams);
        if (didGroups.Data.Count == 0
            || didGroups.Data[0].StockKeepingUnits == null
            || didGroups.Data[0].StockKeepingUnits!.Count == 0)
        {
            Console.WriteLine("  No DID group with SKUs found for this NANPA prefix, skipping.");
            return;
        }

        var sku = didGroups.Data[0].StockKeepingUnits![0];
        Console.WriteLine($"  DID group: {didGroups.Data[0].Id} SKU: {sku.Id} (monthly: {sku.MonthlyPrice})");

        // Step 3: create the order
        var orderItem = new DidOrderItem
        {
            SkuId = sku.Id,
            NanpaPrefixId = nanpaPrefix.Id,
            Qty = 1,
        };

        var order = new Order
        {
            AllowBackOrdering = true,
            Items = new List<OrderItemBase> { orderItem },
        };

        var response = await client.Orders().CreateAsync(order);
        Console.WriteLine($"  Order created: {response.Data.Id} (Status: {response.Data.Status}, Amount: {response.Data.Amount}, Ref: {response.Data.Reference})");
    }
}
