using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

public static class OrdersAvailableDidsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Order Available DID ---");

        // Get available DIDs with included DID group and SKUs
        var queryParams = new QueryParams()
            .Include("did_group.stock_keeping_units");
        var availableDids = await client.AvailableDids().ListAsync(queryParams);

        if (availableDids.Data.Count == 0)
        {
            Console.WriteLine("  No available DIDs found, skipping.");
            return;
        }

        var availableDid = availableDids.Data[0];
        Console.WriteLine($"  Available DID: {availableDid.Number}");

        if (availableDid.DidGroup?.StockKeepingUnits == null
            || availableDid.DidGroup.StockKeepingUnits.Count == 0)
        {
            Console.WriteLine("  No stock_keeping_units found in included did_group, skipping.");
            return;
        }

        var orderItem = new AvailableDidOrderItem
        {
            AvailableDidId = availableDid.Id,
            SkuId = availableDid.DidGroup.StockKeepingUnits[0].Id
        };

        var order = new Order
        {
            Items = new List<OrderItemBase> { orderItem }
        };

        var response = await client.Orders().CreateAsync(order);
        Console.WriteLine($"  Order created: {response.Data.Id} (Status: {response.Data.Status})");
    }
}
