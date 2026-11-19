# Examples

All examples read the API key from the `DIDWW_API_KEY` environment variable.

## Prerequisites

- .NET 9.0+ or .NET 10.0+
- DIDWW API key for sandbox account

## Environment variables

- `DIDWW_API_KEY` (required): your DIDWW API key

## Run an example

Since the project targets multiple frameworks, you must specify one with `--framework`:

```bash
# Run a specific example
DIDWW_API_KEY=your_api_key dotnet run --project examples/Didww.Api3.Examples --framework net10.0 -- balance

# Pass an argument to an example that takes one
DIDWW_API_KEY=your_api_key dotnet run --project examples/Didww.Api3.Examples --framework net10.0 -- prices-download /path/to/prices

# Without an example name, a short tour runs (balance, countries, regions, did-groups, dids,
# trunks, voice-in-trunk-groups, voice-out-trunks, orders, capacity-pools, exports, upload-file, webhook)
DIDWW_API_KEY=your_api_key dotnet run --project examples/Didww.Api3.Examples --framework net10.0
```

You can also use `--framework net9.0` if you have .NET 9 installed.

The process exits with 1 when an example fails or the example name is unknown.

## Available examples

| Example | Command | Description |
|---|---|---|
| [`BalanceExample.cs`](BalanceExample.cs) | `balance` | Fetches and prints current account balance and credit. |
| [`CountriesExample.cs`](CountriesExample.cs) | `countries` | Lists countries and fetches one with included regions. |
| [`RegionsExample.cs`](RegionsExample.cs) | `regions` | Lists regions with filtering, sorting, and includes. |
| [`DidGroupsExample.cs`](DidGroupsExample.cs) | `did-groups` | Lists DID groups with included SKUs, number type, demo flag and pricing details. |
| [`DidHistoryExample.cs`](DidHistoryExample.cs) | `did-history` | Lists DID ownership history (last 90 days). |
| [`DidsExample.cs`](DidsExample.cs) | `dids` | Lists DIDs and demonstrates DID updates (emergency fields and identity). |
| [`DidTrunkAssignmentExample.cs`](DidTrunkAssignmentExample.cs) | `did-trunk-assignment` | Assigns a DID to a trunk, then to a trunk group (assigning one clears the other), and restores the DID's routing. |
| [`DidSmsRoutingExample.cs`](DidSmsRoutingExample.cs) | `did-sms-routing` | Routes a DID's inbound SMS to an SMS trunk, then to an SMS trunk group (exclusive assignment), then restores the DID's original SMS routing. |
| [`DidReservationsExample.cs`](DidReservationsExample.cs) | `did-reservations` | Reserves an available DID, lists and finds reservations, deletes the reservation. |
| [`CapacityPoolsExample.cs`](CapacityPoolsExample.cs) | `capacity-pools` | Lists capacity pools with included shared capacity groups and qty-based pricings, finds one. |
| [`SharedCapacityGroupsExample.cs`](SharedCapacityGroupsExample.cs) | `shared-capacity-groups` | Creates a shared capacity group in a capacity pool, lists, updates and deletes it. |
| [`OrdersExample.cs`](OrdersExample.cs) | `orders` | Lists recent orders, orders a DID by SKU and cancels that order. |
| [`OrdersSkuExample.cs`](OrdersSkuExample.cs) | `orders-sku` | Orders DIDs by SKU and quantity, resolving the SKU from a DID group. |
| [`OrdersAvailableDidsExample.cs`](OrdersAvailableDidsExample.cs) | `orders-available-dids` | Orders a specific available DID with nested includes. |
| [`OrdersReservationDidsExample.cs`](OrdersReservationDidsExample.cs) | `orders-reservation-dids` | Reserves DID then orders via ReservationDidOrderItem. |
| [`OrdersNanpaExample.cs`](OrdersNanpaExample.cs) | `orders-nanpa` | Orders a DID number by NPA/NXX prefix. |
| [`OrdersCapacityExample.cs`](OrdersCapacityExample.cs) | `orders-capacity` | Purchases capacity from a capacity pool. |
| [`OrdersAllItemTypesExample.cs`](OrdersAllItemTypesExample.cs) | `orders-all-item-types` | One order with the three DID item variants: by SKU and quantity, by available DID and by reservation. |
| [`OrdersEmergencyExample.cs`](OrdersEmergencyExample.cs) | `orders-emergency` | Reads server-created emergency orders (recent orders with `emergency_order_items`) and follows an emergency calling service to its order. |
| [`TrunksExample.cs`](TrunksExample.cs) | `trunks` | Lists Voice IN trunks, finds one, creates a SIP and a PSTN trunk, updates both and deletes both. |
| [`VoiceInTrunkGroupsExample.cs`](VoiceInTrunkGroupsExample.cs) | `voice-in-trunk-groups` | Creates two trunks and a trunk group with them, lists groups, renames the group and deletes it with its trunks. |
| [`VoiceInTrunkSipRegistrationExample.cs`](VoiceInTrunkSipRegistrationExample.cs) | `voice-in-trunk-sip-registration` | End-to-end SIP registration flow: create with `EnabledSipRegistration=true`, rename, disable by setting `Host`, re-enable by toggling the flag. The SDK keeps the dependent fields (`Host`, `Port`, `UseDidInRuri`) aligned with the server's validation rules automatically. |
| [`VoiceOutTrunksExample.cs`](VoiceOutTrunksExample.cs) | `voice-out-trunks` | CRUD operations on outbound trunks. |
| [`SmsTrunksExample.cs`](SmsTrunksExample.cs) | `sms-trunks` | Lists SMS trunks, creates an HTTP IN trunk, updates its configuration, regenerates the credentials of an HTTP OUT trunk and deletes both. |
| [`SmsTrunkGroupsExample.cs`](SmsTrunkGroupsExample.cs) | `sms-trunk-groups` | Creates two inbound SMS trunks, groups them by priority, lists groups and deletes the group with its trunks. |
| [`SenderIdVerificationsExample.cs`](SenderIdVerificationsExample.cs) | `sender-id-verifications` | Lists Sender ID verifications with their SMS trunks, routes an active one through an SMS trunk that sends messages and restores its original trunk. |
| [`IdentityAddressProofsExample.cs`](IdentityAddressProofsExample.cs) | `identity-address-proofs` | Creates an identity and an address, uploads an encrypted file, attaches a proof to each, prints their verified flags and deletes them. |
| [`AddressVerificationsExample.cs`](AddressVerificationsExample.cs) | `address-verifications` | Lists address verifications with reject_comment / external_reference_id. |
| [`UploadFileExample.cs`](UploadFileExample.cs) | `upload-file [file]` | Encrypts the file given as argument, or a generated sample PDF, with the public keys, uploads it to `encrypted_files`, prints its ID and deletes it. |
| [`ExportsExample.cs`](ExportsExample.cs) | `exports` | Creates and lists CDR exports, with external_reference_id. |
| [`ExportsSmsExample.cs`](ExportsSmsExample.cs) | `exports-sms` | Creates SMS-In and SMS-Out exports with their filters and reads the columns of a completed one. |
| [`EmergencyRequirementsExample.cs`](EmergencyRequirementsExample.cs) | `emergency-requirements` | Lists emergency service requirements per country/number_type. |
| [`EmergencyRequirementValidationsExample.cs`](EmergencyRequirementValidationsExample.cs) | `emergency-requirement-validations` | Pre-validates an emergency order triple (requirement + address + identity). |
| [`EmergencyVerificationsExample.cs`](EmergencyVerificationsExample.cs) | `emergency-verifications` | Lists and creates emergency verifications. |
| [`EmergencyCallingServicesExample.cs`](EmergencyCallingServicesExample.cs) | `emergency-calling-services` | Lists and cancels customer emergency calling services. |
| [`EmergencyScenarioExample.cs`](EmergencyScenarioExample.cs) | `emergency-scenario` | End-to-end: find DID → check requirements → validate → create verification → get service. |
| [`PricesLookupExample.cs`](PricesLookupExample.cs) | `prices-lookup [number]` | Looks up Voice IN, Voice OUT, SMS OUT (A2P, P2P, OTP) and SMS IN prices for a number. |
| [`PricesDownloadExample.cs`](PricesDownloadExample.cs) | `prices-download [dir]` | Downloads the gzipped Voice IN price file to the directory given as argument, or to `didww-prices` in the user cache directory, re-checks it with If-Modified-Since and reads a few rows. |
| [`WebhookExample.cs`](WebhookExample.cs) | `webhook` | Signs a sample callback the way DIDWW does and validates it with `RequestValidator`: `True` for the callback, `False` for a tampered payload. Needs no API key. |

## Troubleshooting

If `DIDWW_API_KEY` is missing, examples fail with:

```
DIDWW_API_KEY environment variable is not set
```

If you see `Unable to run your project. Your project targets multiple frameworks`, add `--framework net10.0` (or `net9.0`) to the command.
