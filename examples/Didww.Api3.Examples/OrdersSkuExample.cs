using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

// Orders DIDs by SKU and quantity: a DidOrderItem with SkuId and Qty orders
// that many numbers of the SKU's DID group. The SKU is taken from the
// stock_keeping_units included with a DID group.
public static class OrdersSkuExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Order DIDs by SKU ---");

        var queryParams = new QueryParams()
            .Include("stock_keeping_units")
            .Page(1, 1);
        var didGroups = (await client.DidGroups().ListAsync(queryParams)).Data;
        var sku = didGroups.FirstOrDefault()?.StockKeepingUnits?.FirstOrDefault();
        if (sku == null)
        {
            Console.WriteLine("  No DID group with stock_keeping_units found, skipping.");
            return;
        }
        Console.WriteLine($"  Using SKU: {sku.Id} (setup: {sku.SetupPrice}, monthly: {sku.MonthlyPrice})");

        // 2026-04-16 external_reference_id — customer-supplied tag (max 100 chars)
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var order = new Order
        {
            AllowBackOrdering = true,
            Items = new List<OrderItemBase>
            {
                new DidOrderItem { SkuId = sku.Id, Qty = 2 }
            },
            ExternalReferenceId = "dotnet-order-sku-" + suffix
        };

        var created = (await client.Orders().CreateAsync(order)).Data;
        Console.WriteLine($"  Order created: {created.Id} (Status: {created.Status}, Amount: {created.Amount})");
        Console.WriteLine($"    Reference: {created.Reference}");
        Console.WriteLine($"    External reference: {created.ExternalReferenceId}");
        foreach (var item in created.Items ?? new List<OrderItemBase>())
        {
            if (item is DidOrderItem didItem)
                Console.WriteLine($"    Item: {item.ItemType} sku={didItem.SkuId} qty={didItem.Qty}");
            else
                Console.WriteLine($"    Item: {item.ItemType}");
        }
    }
}
