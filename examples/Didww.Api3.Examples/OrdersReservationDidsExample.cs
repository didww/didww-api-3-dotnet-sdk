using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

public static class OrdersReservationDidsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Order via DID Reservation ---");

        // 1. Find an available DID with SKU
        var queryParams = new QueryParams()
            .Include("did_group.stock_keeping_units")
            .Page(1, 1);
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
            Console.WriteLine("  No SKUs found for this DID group, skipping.");
            return;
        }

        // 2. Reserve the DID
        Console.WriteLine("\n--- Reserve DID ---");
        var reservation = new DidReservation
        {
            Description = "Reservation for order example",
            AvailableDid = AvailableDid.Build(availableDid.Id!)
        };

        var reservationResponse = await client.DidReservations().CreateAsync(reservation);
        var createdReservation = reservationResponse.Data;
        Console.WriteLine($"  Reservation: {createdReservation.Id}");
        Console.WriteLine($"    Expires at: {createdReservation.ExpireAt}");

        // 3. Order the reserved DID using ReservationDidOrderItem
        Console.WriteLine("\n--- Order Reserved DID ---");
        var sku = availableDid.DidGroup.StockKeepingUnits[0];
        var orderItem = new ReservationDidOrderItem
        {
            DidReservationId = createdReservation.Id,
            SkuId = sku.Id
        };

        var order = new Order
        {
            Items = new List<OrderItemBase> { orderItem }
        };

        var orderResponse = await client.Orders().CreateAsync(order);
        Console.WriteLine($"  Order created: {orderResponse.Data.Id}");
        Console.WriteLine($"    Status: {orderResponse.Data.Status}");
        Console.WriteLine($"    Amount: {orderResponse.Data.Amount}");
    }
}
