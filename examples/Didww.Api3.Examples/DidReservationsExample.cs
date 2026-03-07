using Didww.Api3.Http;
using Didww.Api3.Resource;

namespace Didww.Api3.Examples;

public static class DidReservationsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Reserve Available DID ---");

        // Find an available DID to reserve
        var available = await client.AvailableDids().ListAsync(new QueryParams().Page(1, 1));
        if (available.Data.Count == 0)
        {
            Console.WriteLine("  No available DIDs found, skipping.");
            return;
        }

        var availableDid = available.Data[0];
        Console.WriteLine($"  Available DID: {availableDid.Number}");

        // Create a reservation
        var reservation = new DidReservation
        {
            Description = "SDK example reservation",
            AvailableDid = AvailableDid.Build(availableDid.Id!)
        };

        var response = await client.DidReservations().CreateAsync(reservation);
        var created = response.Data;
        Console.WriteLine($"  Reservation created: {created.Id}");
        Console.WriteLine($"    Expires at: {created.ExpireAt}");

        Console.WriteLine("\n--- List DID Reservations ---");
        var listResponse = await client.DidReservations().ListAsync();
        foreach (var r in listResponse.Data)
        {
            Console.WriteLine($"  Reservation: {r.Id} (expires: {r.ExpireAt})");
        }

        Console.WriteLine("\n--- Delete DID Reservation ---");
        await client.DidReservations().DeleteAsync(created.Id!);
        Console.WriteLine($"  Deleted: {created.Id}");
    }
}
