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
# Run all examples
DIDWW_API_KEY=your_api_key dotnet run --project examples/Didww.Api3.Examples --framework net10.0

# Run a specific example
DIDWW_API_KEY=your_api_key dotnet run --project examples/Didww.Api3.Examples --framework net10.0 -- balance

# Pass an argument to an example that takes one
DIDWW_API_KEY=your_api_key dotnet run --project examples/Didww.Api3.Examples --framework net10.0 -- prices-download /path/to/prices
```

You can also use `--framework net9.0` if you have .NET 9 installed.

## Available examples

| Example | Command | Description |
|---|---|---|
| [`BalanceExample.cs`](BalanceExample.cs) | `balance` | Fetches and prints current account balance and credit. |
| [`CountriesExample.cs`](CountriesExample.cs) | `countries` | Lists countries and fetches one with included regions. |
| [`RegionsExample.cs`](RegionsExample.cs) | `regions` | Lists regions with filtering, sorting, and includes. |
| [`DidGroupsExample.cs`](DidGroupsExample.cs) | `did-groups` | Lists DID groups with included SKUs, number type, demo flag and pricing details. |
| [`DidHistoryExample.cs`](DidHistoryExample.cs) | `did-history` | Lists DID ownership history (last 90 days). |
| [`DidsExample.cs`](DidsExample.cs) | `dids` | Lists DIDs and demonstrates DID updates (emergency fields and identity). |
| [`TrunksExample.cs`](TrunksExample.cs) | `trunks` | Creates SIP and PSTN trunks, lists and deletes. |
| [`VoiceInTrunkSipRegistrationExample.cs`](VoiceInTrunkSipRegistrationExample.cs) | `voice-in-trunk-sip-registration` | End-to-end SIP registration flow: create with `EnabledSipRegistration=true`, rename, disable by setting `Host`, re-enable by toggling the flag. The SDK keeps the dependent fields (`Host`, `Port`, `UseDidInRuri`) aligned with the server's validation rules automatically. |
| [`TrunkGroupsExample.cs`](TrunkGroupsExample.cs) | `trunk-groups` | Creates trunk group with two trunks, lists, updates, deletes. |
| [`VoiceOutTrunksExample.cs`](VoiceOutTrunksExample.cs) | `voice-out-trunks` | CRUD operations on outbound trunks. |
| [`OrdersExample.cs`](OrdersExample.cs) | `orders` | Orders a DID by resolving SKU from DID group. |
| [`OrdersNanpaExample.cs`](OrdersNanpaExample.cs) | `orders-nanpa` | Orders a DID number by NPA/NXX prefix. |
| [`OrdersAvailableDidsExample.cs`](OrdersAvailableDidsExample.cs) | `orders-available` | Orders a specific available DID with nested includes. |
| [`OrdersCapacityExample.cs`](OrdersCapacityExample.cs) | `orders-capacity` | Purchases capacity from a capacity pool. |
| [`CapacityPoolsExample.cs`](CapacityPoolsExample.cs) | `capacity-pools` | Lists capacity pools with shared groups and pricings. |
| [`SharedCapacityGroupsExample.cs`](SharedCapacityGroupsExample.cs) | `shared-capacity-groups` | CRUD shared capacity group from pool. |
| [`DidReservationsExample.cs`](DidReservationsExample.cs) | `did-reservations` | Reserves an available DID, lists and deletes. |
| [`DidTrunkAssignmentExample.cs`](DidTrunkAssignmentExample.cs) | `did-trunk-assignment` | Creates trunk, assigns to DID, shows exclusivity. |
| [`IdentityAddressProofsExample.cs`](IdentityAddressProofsExample.cs) | `identity-address-proofs` | Creates identity, address, lists proof types (birth_country). |
| [`AddressVerificationsExample.cs`](AddressVerificationsExample.cs) | `address-verifications` | Lists address verifications with reject_comment / external_reference_id. |
| [`OrdersAllItemTypesExample.cs`](OrdersAllItemTypesExample.cs) | `orders-all-items` | Demonstrates all 3 order item types in one example. |
| [`OrdersReservationDidsExample.cs`](OrdersReservationDidsExample.cs) | `orders-reservation` | Reserves DID then orders via ReservationDidOrderItem. |
| [`ExportsExample.cs`](ExportsExample.cs) | `exports` | Creates and lists CDR exports, with external_reference_id. |
| [`EncryptionExample.cs`](EncryptionExample.cs) | `encryption` | Encrypts a file and uploads to `encrypted_files`. |
| [`WebhookExample.cs`](WebhookExample.cs) | `webhook` | Demonstrates webhook callback signature validation. |

### SMS

| Example | Command | Description |
|---|---|---|
| [`SmsTrunksExample.cs`](SmsTrunksExample.cs) | `sms-trunks` | Lists SMS trunks, creates an HTTP IN trunk, updates its configuration, regenerates the credentials of an HTTP OUT trunk and deletes both. |
| [`SmsTrunkGroupsExample.cs`](SmsTrunkGroupsExample.cs) | `sms-trunk-groups` | Creates two inbound SMS trunks, groups them by priority, lists groups and deletes the group with its trunks. |
| [`SenderIdVerificationsExample.cs`](SenderIdVerificationsExample.cs) | `sender-id-verifications` | Lists Sender ID verifications with their SMS trunks, routes an active one through an SMS trunk that sends messages and restores its original trunk. |
| [`DidSmsRoutingExample.cs`](DidSmsRoutingExample.cs) | `did-sms-routing` | Routes a DID's inbound SMS to an SMS trunk, then to an SMS trunk group (exclusive assignment), then restores the DID's original SMS routing. |
| [`ExportsSmsExample.cs`](ExportsSmsExample.cs) | `exports-sms` | Creates SMS-In and SMS-Out exports with their filters and reads the columns of a completed one. |

### Prices

| Example | Command | Description |
|---|---|---|
| [`PricesLookupExample.cs`](PricesLookupExample.cs) | `prices-lookup [number]` | Looks up Voice IN, Voice OUT, SMS OUT (A2P, P2P, OTP) and SMS IN prices for a number. |
| [`PricesDownloadExample.cs`](PricesDownloadExample.cs) | `prices-download [dir]` | Downloads the gzipped Voice IN price file to the directory given as argument, or to `didww-prices` in the user cache directory, re-checks it with If-Modified-Since and reads a few rows. |

### Emergency Services

| Example | Command | Description |
|---|---|---|
| [`EmergencyRequirementsExample.cs`](EmergencyRequirementsExample.cs) | `emergency-requirements` | Lists emergency service requirements per country/number_type. |
| [`EmergencyCallingServicesExample.cs`](EmergencyCallingServicesExample.cs) | `emergency-calling-services` | Lists and cancels customer emergency calling services. |
| [`EmergencyVerificationsExample.cs`](EmergencyVerificationsExample.cs) | `emergency-verifications` | Lists and creates emergency verifications. |
| [`EmergencyRequirementValidationsExample.cs`](EmergencyRequirementValidationsExample.cs) | `emergency-requirement-validations` | Pre-validates an emergency order triple (requirement + address + identity). |
| [`EmergencyScenarioExample.cs`](EmergencyScenarioExample.cs) | `emergency-scenario` | End-to-end: find DID → check requirements → validate → create verification → get service. |
| [`OrdersEmergencyExample.cs`](OrdersEmergencyExample.cs) | `orders-emergency` | Inspects server-created Emergency orders and `emergency_order_items`. |

## Troubleshooting

If `DIDWW_API_KEY` is missing, examples fail with:

```
DIDWW_API_KEY environment variable is not set
```

If you see `Unable to run your project. Your project targets multiple frameworks`, add `--framework net10.0` (or `net9.0`) to the command.
