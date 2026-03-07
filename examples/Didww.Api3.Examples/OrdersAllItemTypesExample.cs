using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

public static class OrdersAllItemTypesExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Order with All Item Types ---");
        Console.WriteLine("  Demonstrates DidOrderItem, AvailableDidOrderItem, and CapacityOrderItem.\n");

        // 1. DidOrderItem - order by SKU
        Console.WriteLine("--- 1. DidOrderItem (order by SKU) ---");
        var queryParams = new QueryParams()
            .Include("stock_keeping_units")
            .Page(1, 1);
        var didGroups = await client.DidGroups().ListAsync(queryParams);

        if (didGroups.Data.Count == 0
            || didGroups.Data[0].StockKeepingUnits == null
            || didGroups.Data[0].StockKeepingUnits.Count == 0)
        {
            Console.WriteLine("  No DID group with SKUs found, skipping DidOrderItem.");
        }
        else
        {
            var sku = didGroups.Data[0].StockKeepingUnits[0];
            Console.WriteLine($"  SKU: {sku.Id}");

            var didItem = new DidOrderItem { SkuId = sku.Id, Qty = 1 };
            var order = new Order
            {
                AllowBackOrdering = true,
                Items = new List<OrderItemBase> { didItem }
            };

            var response = await client.Orders().CreateAsync(order);
            Console.WriteLine($"  Order created: {response.Data.Id} (Status: {response.Data.Status})");
        }

        // 2. AvailableDidOrderItem - order specific available DID
        Console.WriteLine("\n--- 2. AvailableDidOrderItem (order specific DID) ---");
        var availableParams = new QueryParams()
            .Include("did_group.stock_keeping_units")
            .Page(1, 1);
        var availableDids = await client.AvailableDids().ListAsync(availableParams);

        if (availableDids.Data.Count == 0
            || availableDids.Data[0].DidGroup?.StockKeepingUnits == null
            || availableDids.Data[0].DidGroup.StockKeepingUnits.Count == 0)
        {
            Console.WriteLine("  No available DID with SKUs found, skipping AvailableDidOrderItem.");
        }
        else
        {
            var did = availableDids.Data[0];
            var avSku = did.DidGroup!.StockKeepingUnits![0];
            Console.WriteLine($"  Available DID: {did.Number}, SKU: {avSku.Id}");

            var avItem = new AvailableDidOrderItem
            {
                AvailableDidId = did.Id,
                SkuId = avSku.Id
            };
            var order = new Order
            {
                Items = new List<OrderItemBase> { avItem }
            };

            var response = await client.Orders().CreateAsync(order);
            Console.WriteLine($"  Order created: {response.Data.Id} (Status: {response.Data.Status})");
        }

        // 3. CapacityOrderItem - purchase capacity
        Console.WriteLine("\n--- 3. CapacityOrderItem (purchase capacity) ---");
        var pools = await client.CapacityPools().ListAsync(new QueryParams().Page(1, 1));
        if (pools.Data.Count == 0)
        {
            Console.WriteLine("  No capacity pools found, skipping CapacityOrderItem.");
        }
        else
        {
            var pool = pools.Data[0];
            Console.WriteLine($"  Capacity Pool: {pool.Name} (min qty: {pool.MinimumQtyPerOrder})");

            var capItem = new CapacityOrderItem
            {
                CapacityPoolId = pool.Id,
                Qty = pool.MinimumQtyPerOrder ?? 1
            };
            var order = new Order
            {
                Items = new List<OrderItemBase> { capItem }
            };

            var response = await client.Orders().CreateAsync(order);
            Console.WriteLine($"  Order created: {response.Data.Id} (Status: {response.Data.Status})");
        }
    }
}
