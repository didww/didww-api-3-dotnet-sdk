using Didww.Api3.Examples;

// DIDWW API v3 .NET SDK Examples
// Set DIDWW_API_KEY environment variable before running.
// Usage: dotnet run --framework net10.0 [example-name] [argument]
// Available examples: balance, countries, regions, did-groups, dids, trunks,
//   trunk-groups, voice-out-trunks, orders, orders-nanpa, orders-available, orders-capacity,
//   orders-all-items, orders-reservation, capacity-pools, shared-capacity-groups,
//   did-reservations, did-trunk-assignment, identity-address-proofs,
//   exports, encryption, webhook,
//   did-history, emergency-requirements, emergency-calling-services,
//   emergency-verifications, emergency-requirement-validations,
//   address-verifications, orders-emergency, emergency-scenario, voice-in-trunk-groups,
//   sms-trunks, sms-trunk-groups, sender-id-verifications, did-sms-routing, exports-sms,
//   prices-lookup [number], prices-download [dir]

var exampleName = args.Length > 0 ? args[0].ToLower() : "all";
var exampleArgument = args.Length > 1 ? args[1] : null;
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
        case "orders-nanpa":
            await OrdersNanpaExample.RunAsync(client!);
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
        case "shared-capacity-groups":
            await SharedCapacityGroupsExample.RunAsync(client!);
            break;
        case "did-reservations":
            await DidReservationsExample.RunAsync(client!);
            break;
        case "did-trunk-assignment":
            await DidTrunkAssignmentExample.RunAsync(client!);
            break;
        case "identity-address-proofs":
            await IdentityAddressProofsExample.RunAsync(client!);
            break;
        case "orders-all-items":
            await OrdersAllItemTypesExample.RunAsync(client!);
            break;
        case "orders-reservation":
            await OrdersReservationDidsExample.RunAsync(client!);
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
        case "did-history":
            await DidHistoryExample.RunAsync(client!);
            break;
        case "emergency-requirements":
            await EmergencyRequirementsExample.RunAsync(client!);
            break;
        case "emergency-calling-services":
            await EmergencyCallingServicesExample.RunAsync(client!);
            break;
        case "emergency-verifications":
            await EmergencyVerificationsExample.RunAsync(client!);
            break;
        case "emergency-requirement-validations":
            await EmergencyRequirementValidationsExample.RunAsync(client!);
            break;
        case "address-verifications":
            await AddressVerificationsExample.RunAsync(client!);
            break;
        case "orders-emergency":
            await OrdersEmergencyExample.RunAsync(client!);
            break;
        case "emergency-scenario":
            await EmergencyScenarioExample.RunAsync(client!);
            break;
        case "voice-in-trunk-groups":
            await VoiceInTrunkGroupsExample.RunAsync(client!);
            break;
        case "voice-in-trunk-sip-registration":
            await VoiceInTrunkSipRegistrationExample.RunAsync(client!);
            break;
        case "sms-trunks":
            await SmsTrunksExample.RunAsync(client!);
            break;
        case "sms-trunk-groups":
            await SmsTrunkGroupsExample.RunAsync(client!);
            break;
        case "sender-id-verifications":
            await SenderIdVerificationsExample.RunAsync(client!);
            break;
        case "did-sms-routing":
            await DidSmsRoutingExample.RunAsync(client!);
            break;
        case "exports-sms":
            await ExportsSmsExample.RunAsync(client!);
            break;
        case "prices-lookup":
            await PricesLookupExample.RunAsync(client!, exampleArgument);
            break;
        case "prices-download":
            await PricesDownloadExample.RunAsync(client!, exampleArgument);
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
            Console.WriteLine("  trunk-groups, voice-out-trunks, orders, orders-nanpa, orders-available,");
            Console.WriteLine("  orders-capacity, orders-all-items, orders-reservation,");
            Console.WriteLine("  capacity-pools, shared-capacity-groups, did-reservations,");
            Console.WriteLine("  did-trunk-assignment, identity-address-proofs,");
            Console.WriteLine("  exports, encryption, webhook,");
            Console.WriteLine("  did-history, emergency-requirements, emergency-calling-services,");
            Console.WriteLine("  emergency-verifications, emergency-requirement-validations,");
            Console.WriteLine("  address-verifications, orders-emergency, emergency-scenario,");
            Console.WriteLine("  voice-in-trunk-groups, sms-trunks, sms-trunk-groups,");
            Console.WriteLine("  sender-id-verifications, did-sms-routing, exports-sms,");
            Console.WriteLine("  prices-lookup [number], prices-download [dir], all");
            break;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
