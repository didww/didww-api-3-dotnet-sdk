using Didww.Api3.Http;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// Lists and cancels customer Emergency Calling Services (2026-04-16).
//
// An EmergencyCallingService represents a customer's 911/112 subscription
// attached to one or more DIDs. It ties an address, identity, number type
// and country together with a pricing snapshot (meta.setup_price, meta.monthly_price).
//
// Supported operations: index, show, destroy (cancel).
public static class EmergencyCallingServicesExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("=== Emergency Calling Services ===");
        var queryParams = new QueryParams().Include("country", "dids");
        var response = await client.EmergencyCallingServices().ListAsync(queryParams);
        var services = response.Data;
        Console.WriteLine($"Found {services.Count} emergency calling services");

        foreach (var svc in services.Take(5))
        {
            Console.WriteLine($"\nService: {svc.Id}");
            Console.WriteLine($"  Name: {svc.Name}");
            Console.WriteLine($"  Reference: {svc.Reference}");
            Console.WriteLine($"  Status: {svc.Status}");
            Console.WriteLine($"  Country: {svc.Country?.Name}");
            Console.WriteLine($"  Number Type: {svc.NumberType}");
            Console.WriteLine($"  Activated: {svc.ActivatedAt}");
            if (svc.CanceledAt.HasValue)
                Console.WriteLine($"  Canceled: {svc.CanceledAt}");
            if (svc.RenewDate != null)
                Console.WriteLine($"  Renews: {svc.RenewDate}");
            if (svc.Dids != null && svc.Dids.Count > 0)
                Console.WriteLine($"  Attached DIDs: {string.Join(", ", svc.Dids.Select(d => d.Number))}");
            if (svc.Meta != null)
            {
                Console.WriteLine($"  Setup price: {svc.Meta.GetValueOrDefault("setup_price")}");
                Console.WriteLine($"  Monthly price: {svc.Meta.GetValueOrDefault("monthly_price")}");
            }
        }

        // Filter by status
        Console.WriteLine("\n=== Only active emergency calling services ===");
        var activeParams = new QueryParams().Filter("status", "active");
        var active = await client.EmergencyCallingServices().ListAsync(activeParams);
        Console.WriteLine($"Found {active.Data.Count} active services");

        // Cancel a service (destroy) — uncomment to try:
        //
        // var toCancel = services.FirstOrDefault(s => s.IsActive);
        // if (toCancel != null)
        // {
        //     Console.WriteLine($"\nCanceling service {toCancel.Id}...");
        //     await client.EmergencyCallingServices().DeleteAsync(toCancel.Id!);
        //     Console.WriteLine("Service canceled");
        // }

        Console.WriteLine("\nAvailable statuses: " +
            string.Join(", ", Enum.GetNames<EmergencyCallingServiceStatus>()));
    }
}
