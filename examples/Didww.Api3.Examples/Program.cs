using Didww.Api3.Examples;

// DIDWW API v3 .NET SDK Examples
// Set DIDWW_API_KEY environment variable before running.
// Usage: dotnet run --framework net10.0 [example-name]
// Available examples: balance, countries, regions, did-groups, dids, trunks,
//   trunk-groups, voice-out-trunks, orders, orders-available, orders-capacity,
//   capacity-pools, exports, encryption, webhook

var exampleName = args.Length > 0 ? args[0].ToLower() : "all";
var client = exampleName != "webhook" ? ExampleClientFactory.Create() : null;

try
{
    switch (exampleName)
    {
        case "balance":
            await BalanceExample.RunAsync(client!);
            break;
        case "countries":
            await CountriesExample.RunAsync(client!);
            break;
        case "regions":
            await RegionsExample.RunAsync(client!);
            break;
        case "did-groups":
            await DidGroupsExample.RunAsync(client!);
            break;
        case "dids":
            await DidsExample.RunAsync(client!);
            break;
        case "trunks":
            await TrunksExample.RunAsync(client!);
            break;
        case "trunk-groups":
            await TrunkGroupsExample.RunAsync(client!);
            break;
        case "voice-out-trunks":
            await VoiceOutTrunksExample.RunAsync(client!);
            break;
        case "orders":
            await OrdersExample.RunAsync(client!);
            break;
        case "orders-available":
            await OrdersAvailableDidsExample.RunAsync(client!);
            break;
        case "orders-capacity":
            await OrdersCapacityExample.RunAsync(client!);
            break;
        case "capacity-pools":
            await CapacityPoolsExample.RunAsync(client!);
            break;
        case "exports":
            await ExportsExample.RunAsync(client!);
            break;
        case "encryption":
            await EncryptionExample.RunAsync(client!);
            break;
        case "webhook":
            WebhookExample.Run();
            break;
        case "all":
            await BalanceExample.RunAsync(client!);
            Console.WriteLine();
            await CountriesExample.RunAsync(client!);
            Console.WriteLine();
            await RegionsExample.RunAsync(client!);
            Console.WriteLine();
            await DidGroupsExample.RunAsync(client!);
            Console.WriteLine();
            await DidsExample.RunAsync(client!);
            Console.WriteLine();
            await TrunksExample.RunAsync(client!);
            Console.WriteLine();
            await TrunkGroupsExample.RunAsync(client!);
            Console.WriteLine();
            await VoiceOutTrunksExample.RunAsync(client!);
            Console.WriteLine();
            await OrdersExample.RunAsync(client!);
            Console.WriteLine();
            await CapacityPoolsExample.RunAsync(client!);
            Console.WriteLine();
            await ExportsExample.RunAsync(client!);
            Console.WriteLine();
            await EncryptionExample.RunAsync(client!);
            Console.WriteLine();
            WebhookExample.Run();
            break;
        default:
            Console.WriteLine($"Unknown example: {exampleName}");
            Console.WriteLine("Available: balance, countries, regions, did-groups, dids, trunks,");
            Console.WriteLine("  trunk-groups, voice-out-trunks, orders, orders-available,");
            Console.WriteLine("  orders-capacity, capacity-pools, exports, encryption, webhook, all");
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
