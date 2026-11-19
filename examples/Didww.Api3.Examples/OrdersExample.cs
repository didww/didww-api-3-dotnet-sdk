using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

// Lists recent orders, orders a DID by SKU and cancels that order: deleting
// an order cancels it.
public static class OrdersExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Recent Orders ---");
        var recent = (await client.Orders().ListAsync(new QueryParams().Sort("-created_at").Page(1, 5))).Data;
        Console.WriteLine($"  Found {recent.Count} orders");
        foreach (var o in recent)
        {
            Console.WriteLine($"  Order: {o.Id} (Status: {o.Status}, Amount: {o.Amount})");
            var itemTypes = (o.Items ?? new List<OrderItemBase>()).Select(i => i.ItemType);
            Console.WriteLine($"    Items: {string.Join(", ", itemTypes)}");
            if (o.ExternalReferenceId != null)
                Console.WriteLine($"    External reference: {o.ExternalReferenceId}");
        }

        Console.WriteLine("\n--- Order a DID by SKU ---");
        var groups = (await client.DidGroups().ListAsync(new QueryParams()
            .Include("stock_keeping_units")
            .Page(1, 10))).Data;
        var sku = groups.Select(g => g.StockKeepingUnits?.FirstOrDefault()).FirstOrDefault(s => s != null);
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
            Items = new List<OrderItemBase> { new DidOrderItem { SkuId = sku.Id, Qty = 1 } },
            ExternalReferenceId = "dotnet-order-" + suffix
        };
        var created = (await client.Orders().CreateAsync(order)).Data;
        Console.WriteLine($"  Order created: {created.Id} (Status: {created.Status}, Amount: {created.Amount})");
        Console.WriteLine($"    Reference: {created.Reference}");
        Console.WriteLine($"    External reference: {created.ExternalReferenceId}");

        Console.WriteLine("\n--- Cancel the Order ---");
        await client.Orders().DeleteAsync(created.Id!);
        Console.WriteLine($"  Order canceled: {created.Id}");
    }
}
