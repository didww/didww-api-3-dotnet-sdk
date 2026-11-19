using Didww.Api3.Http;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

// Inspects Emergency orders (2026-04-16).
//
// Emergency orders are created server-side when an EmergencyCallingService
// is activated or renewed — customers cannot POST them directly. They
// appear in GET /orders alongside DID/capacity/NANPA orders.
//
// Each Emergency order carries items of type "emergency_order_items":
//   * qty, emergency_calling_service_id  (request)
//   * nrc, mrc, prorated_mrc, billed_from, billed_to  (response)
public static class OrdersEmergencyExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("=== Recent Orders (last 50, filtering for Emergency) ===");
        var ordersParams = new QueryParams().Sort("-created_at").Page(1, 50);
        var ordersResponse = await client.Orders().ListAsync(ordersParams);
        var orders = ordersResponse.Data;
        // An emergency order is one with emergency order items
        var emergencyOrders = orders
            .Where(o => o.Items?.Any(item => item is EmergencyOrderItem) == true)
            .ToList();
        Console.WriteLine($"Found {emergencyOrders.Count} emergency orders out of {orders.Count} total");

        foreach (var order in emergencyOrders.Take(5))
        {
            Console.WriteLine($"\nOrder: {order.Id}");
            Console.WriteLine($"  Reference: {order.Reference}");
            Console.WriteLine($"  Status: {order.Status}");
            Console.WriteLine($"  Amount: {order.Amount}");
            Console.WriteLine($"  Created: {order.CreatedAt}");
            if (order.ExternalReferenceId != null)
                Console.WriteLine($"  External Reference: {order.ExternalReferenceId}");

            if (order.Items != null)
            {
                for (var i = 0; i < order.Items.Count; i++)
                {
                    if (order.Items[i] is EmergencyOrderItem item)
                    {
                        Console.WriteLine($"  Item #{i + 1} (emergency_order_items):");
                        Console.WriteLine($"    Qty: {item.Qty}");
                        Console.WriteLine($"    Emergency Calling Service ID: {item.EmergencyCallingServiceId}");
                        Console.WriteLine($"    NRC: {item.Nrc}");
                        Console.WriteLine($"    MRC: {item.Mrc}");
                        Console.WriteLine($"    Prorated MRC: {item.ProratedMrc}");
                        Console.WriteLine($"    Billed From: {item.BilledFrom}");
                        Console.WriteLine($"    Billed To:   {item.BilledTo}");
                    }
                }
            }
        }

        // Follow the link from an EmergencyCallingService to its order (if any)
        Console.WriteLine("\n=== Emergency Calling Service -> Order ===");
        var ecsParams = new QueryParams().Include("order").Page(1, 1);
        var ecsResponse = await client.EmergencyCallingServices().ListAsync(ecsParams);
        var svc = ecsResponse.Data.FirstOrDefault();
        if (svc != null)
        {
            Console.WriteLine($"ECS {svc.Id} ({svc.Name})");
            if (svc.Order != null)
                Console.WriteLine($"  -> Order {svc.Order.Id} — status: {svc.Order.Status}, amount: {svc.Order.Amount}");
            else
                Console.WriteLine("  -> No order linked yet");
        }
        else
        {
            Console.WriteLine("No emergency_calling_services on this account");
        }
    }
}
