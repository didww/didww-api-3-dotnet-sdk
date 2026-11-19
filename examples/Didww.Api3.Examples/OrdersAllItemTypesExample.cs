using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

// Creates ONE order with the three DID item variants:
//   1. DidOrderItem            - by SKU and quantity (numbers of a DID group)
//   2. AvailableDidOrderItem   - a specific available DID
//   3. ReservationDidOrderItem - a previously reserved DID
// All three are did_order_items on the wire, so they fit in one order. Then it
// lists the DIDs of the order.
//
// A capacity item cannot share an order with DID items (the API answers
// "items - should have same type"); capacity is ordered on its own, see
// OrdersCapacityExample.
public static class OrdersAllItemTypesExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Order with All DID Item Types ---");

        // Available DIDs with their DID group and its SKUs
        var availableParams = new QueryParams().Include("did_group.stock_keeping_units");
        var availableDids = (await client.AvailableDids().ListAsync(availableParams)).Data
            .Where(ad => ad.DidGroup?.StockKeepingUnits is { Count: > 0 })
            .ToList();
        if (availableDids.Count < 2)
        {
            Console.WriteLine("  Need at least 2 available DIDs with SKUs, skipping.");
            return;
        }
        var ad1 = availableDids[0];
        var ad2 = availableDids[1];
        var skuId1 = ad1.DidGroup!.StockKeepingUnits![0].Id;
        var skuId2 = ad2.DidGroup!.StockKeepingUnits![0].Id;
        Console.WriteLine($"  Available DID 1: {ad1.Number} (sku {skuId1}) - for AvailableDidOrderItem");
        Console.WriteLine($"  Available DID 2: {ad2.Number} (sku {skuId2}) - for the reservation");

        // A SKU for the item by quantity
        var groupParams = new QueryParams().Include("stock_keeping_units").Page(1, 10);
        var groups = (await client.DidGroups().ListAsync(groupParams)).Data;
        var skuForQty = groups.Select(g => g.StockKeepingUnits?.FirstOrDefault()).FirstOrDefault(s => s != null);
        if (skuForQty == null)
        {
            Console.WriteLine("  No DID group with stock_keeping_units found, skipping.");
            return;
        }
        Console.WriteLine($"  SKU for the qty item: {skuForQty.Id}");

        // Reserve the second available DID
        var reservation = (await client.DidReservations().CreateAsync(new DidReservation
        {
            Description = "Reserved for order example",
            AvailableDid = AvailableDid.Build(ad2.Id!)
        })).Data;
        Console.WriteLine($"  Reservation: {reservation.Id}");

        var order = new Order
        {
            Items = new List<OrderItemBase>
            {
                new DidOrderItem { SkuId = skuForQty.Id, Qty = 1 },
                new AvailableDidOrderItem { SkuId = skuId1, AvailableDidId = ad1.Id },
                new ReservationDidOrderItem { SkuId = skuId2, DidReservationId = reservation.Id }
            }
        };

        Order created;
        try
        {
            created = (await client.Orders().CreateAsync(order)).Data;
        }
        catch
        {
            // The order did not use the reservation: release it
            await client.DidReservations().DeleteAsync(reservation.Id!);
            throw;
        }

        Console.WriteLine($"\n  Order ID: {created.Id}");
        Console.WriteLine($"  Amount: {created.Amount}");
        Console.WriteLine($"  Status: {created.Status}");
        Console.WriteLine($"  Created at: {created.CreatedAt}");
        Console.WriteLine($"  Reference: {created.Reference}");
        var items = created.Items ?? new List<OrderItemBase>();
        Console.WriteLine($"  Items count: {items.Count}");
        for (var i = 0; i < items.Count; i++)
            Console.WriteLine($"    Item {i + 1}: type={items[i].ItemType}");

        var dids = (await client.Dids().ListAsync(new QueryParams().Filter("order.id", created.Id!))).Data;
        Console.WriteLine($"\n  DIDs in order ({dids.Count}):");
        foreach (var did in dids)
            Console.WriteLine($"    {did.Id} | {did.Number} | capacity_limit={did.CapacityLimit}");
    }
}
