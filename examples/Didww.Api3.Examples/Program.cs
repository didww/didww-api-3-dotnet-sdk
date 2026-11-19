using Didww.Api3;
using Didww.Api3.Examples;

// DIDWW API v3 .NET SDK Examples
// Set DIDWW_API_KEY environment variable before running.
// Usage: dotnet run --framework net10.0 [example-name] [argument]
// Available examples: balance, countries, regions, did-groups, did-history, dids,
//   did-trunk-assignment, did-sms-routing, did-reservations, capacity-pools,
//   shared-capacity-groups, orders, orders-sku, orders-available-dids,
//   orders-reservation-dids, orders-nanpa, orders-capacity, orders-all-item-types,
//   orders-emergency, trunks, voice-in-trunk-groups, voice-in-trunk-sip-registration,
//   voice-out-trunks, sms-trunks, sms-trunk-groups, sender-id-verifications,
//   identity-address-proofs, address-verifications, upload-file [file], exports,
//   exports-sms, emergency-requirements, emergency-requirement-validations,
//   emergency-verifications, emergency-calling-services, emergency-scenario,
//   prices-lookup [number], prices-download [dir], webhook
// Without an example name a short tour runs ("all").
// The process exits with 1 when an example fails.

var exampleName = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
var exampleArgument = args.Length > 1 ? args[1] : null;

// Created on first use, so that the webhook example runs without an API key.
DidwwClient? client = null;
DidwwClient Client() => client ??= ExampleClientFactory.Create();

try
{
    switch (exampleName)
    {
        case "balance":
            await BalanceExample.RunAsync(Client());
            break;
        case "countries":
            await CountriesExample.RunAsync(Client());
            break;
        case "regions":
            await RegionsExample.RunAsync(Client());
            break;
        case "did-groups":
            await DidGroupsExample.RunAsync(Client());
            break;
        case "did-history":
            await DidHistoryExample.RunAsync(Client());
            break;
        case "dids":
            await DidsExample.RunAsync(Client());
            break;
        case "did-trunk-assignment":
            await DidTrunkAssignmentExample.RunAsync(Client());
            break;
        case "did-sms-routing":
            await DidSmsRoutingExample.RunAsync(Client());
            break;
        case "did-reservations":
            await DidReservationsExample.RunAsync(Client());
            break;
        case "capacity-pools":
            await CapacityPoolsExample.RunAsync(Client());
            break;
        case "shared-capacity-groups":
            await SharedCapacityGroupsExample.RunAsync(Client());
            break;
        case "orders":
            await OrdersExample.RunAsync(Client());
            break;
        case "orders-sku":
            await OrdersSkuExample.RunAsync(Client());
            break;
        case "orders-available-dids":
            await OrdersAvailableDidsExample.RunAsync(Client());
            break;
        case "orders-reservation-dids":
            await OrdersReservationDidsExample.RunAsync(Client());
            break;
        case "orders-nanpa":
            await OrdersNanpaExample.RunAsync(Client());
            break;
        case "orders-capacity":
            await OrdersCapacityExample.RunAsync(Client());
            break;
        case "orders-all-item-types":
            await OrdersAllItemTypesExample.RunAsync(Client());
            break;
        case "orders-emergency":
            await OrdersEmergencyExample.RunAsync(Client());
            break;
        case "trunks":
            await TrunksExample.RunAsync(Client());
            break;
        case "voice-in-trunk-groups":
            await VoiceInTrunkGroupsExample.RunAsync(Client());
            break;
        case "voice-in-trunk-sip-registration":
            await VoiceInTrunkSipRegistrationExample.RunAsync(Client());
            break;
        case "voice-out-trunks":
            await VoiceOutTrunksExample.RunAsync(Client());
            break;
        case "sms-trunks":
            await SmsTrunksExample.RunAsync(Client());
            break;
        case "sms-trunk-groups":
            await SmsTrunkGroupsExample.RunAsync(Client());
            break;
        case "sender-id-verifications":
            await SenderIdVerificationsExample.RunAsync(Client());
            break;
        case "identity-address-proofs":
            await IdentityAddressProofsExample.RunAsync(Client());
            break;
        case "address-verifications":
            await AddressVerificationsExample.RunAsync(Client());
            break;
        case "upload-file":
            await UploadFileExample.RunAsync(Client());
            break;
        case "exports":
            await ExportsExample.RunAsync(Client());
            break;
        case "exports-sms":
            await ExportsSmsExample.RunAsync(Client());
            break;
        case "emergency-requirements":
            await EmergencyRequirementsExample.RunAsync(Client());
            break;
        case "emergency-requirement-validations":
            await EmergencyRequirementValidationsExample.RunAsync(Client());
            break;
        case "emergency-verifications":
            await EmergencyVerificationsExample.RunAsync(Client());
            break;
        case "emergency-calling-services":
            await EmergencyCallingServicesExample.RunAsync(Client());
            break;
        case "emergency-scenario":
            await EmergencyScenarioExample.RunAsync(Client());
            break;
        case "prices-lookup":
            await PricesLookupExample.RunAsync(Client(), exampleArgument);
            break;
        case "prices-download":
            await PricesDownloadExample.RunAsync(Client(), exampleArgument);
            break;
        case "webhook":
            WebhookExample.Run();
            break;
        case "all":
            await BalanceExample.RunAsync(Client());
            Console.WriteLine();
            await CountriesExample.RunAsync(Client());
            Console.WriteLine();
            await RegionsExample.RunAsync(Client());
            Console.WriteLine();
            await DidGroupsExample.RunAsync(Client());
            Console.WriteLine();
            await DidsExample.RunAsync(Client());
            Console.WriteLine();
            await TrunksExample.RunAsync(Client());
            Console.WriteLine();
            await VoiceInTrunkGroupsExample.RunAsync(Client());
            Console.WriteLine();
            await VoiceOutTrunksExample.RunAsync(Client());
            Console.WriteLine();
            await OrdersExample.RunAsync(Client());
            Console.WriteLine();
            await CapacityPoolsExample.RunAsync(Client());
            Console.WriteLine();
            await ExportsExample.RunAsync(Client());
            Console.WriteLine();
            await UploadFileExample.RunAsync(Client());
            Console.WriteLine();
            WebhookExample.Run();
            break;
        default:
            Console.WriteLine($"Unknown example: {exampleName}");
            Console.WriteLine("Available: balance, countries, regions, did-groups, did-history, dids,");
            Console.WriteLine("  did-trunk-assignment, did-sms-routing, did-reservations, capacity-pools,");
            Console.WriteLine("  shared-capacity-groups, orders, orders-sku, orders-available-dids,");
            Console.WriteLine("  orders-reservation-dids, orders-nanpa, orders-capacity, orders-all-item-types,");
            Console.WriteLine("  orders-emergency, trunks, voice-in-trunk-groups, voice-in-trunk-sip-registration,");
            Console.WriteLine("  voice-out-trunks, sms-trunks, sms-trunk-groups, sender-id-verifications,");
            Console.WriteLine("  identity-address-proofs, address-verifications, upload-file [file], exports,");
            Console.WriteLine("  exports-sms, emergency-requirements, emergency-requirement-validations,");
            Console.WriteLine("  emergency-verifications, emergency-calling-services, emergency-scenario,");
            Console.WriteLine("  prices-lookup [number], prices-download [dir], webhook, all");
            return 1;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
    return 1;
}

return 0;
