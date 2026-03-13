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
```

You can also use `--framework net9.0` if you have .NET 9 installed.

## Available examples

| Example | Command | Description |
|---|---|---|
| [`BalanceExample.cs`](BalanceExample.cs) | `balance` | Fetches and prints current account balance and credit. |
| [`CountriesExample.cs`](CountriesExample.cs) | `countries` | Lists countries and fetches one with included regions. |
| [`RegionsExample.cs`](RegionsExample.cs) | `regions` | Lists regions with filtering, sorting, and includes. |
| [`DidGroupsExample.cs`](DidGroupsExample.cs) | `did-groups` | Lists DID groups with included SKUs and pricing details. |
| [`DidsExample.cs`](DidsExample.cs) | `dids` | Lists DIDs and demonstrates DID updates. |
| [`TrunksExample.cs`](TrunksExample.cs) | `trunks` | Creates SIP and PSTN trunks, lists and deletes. |
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
| [`IdentityAddressProofsExample.cs`](IdentityAddressProofsExample.cs) | `identity-address-proofs` | Creates identity, address, lists proof types. |
| [`OrdersAllItemTypesExample.cs`](OrdersAllItemTypesExample.cs) | `orders-all-items` | Demonstrates all 3 order item types in one example. |
| [`OrdersReservationDidsExample.cs`](OrdersReservationDidsExample.cs) | `orders-reservation` | Reserves DID then orders via ReservationDidOrderItem. |
| [`ExportsExample.cs`](ExportsExample.cs) | `exports` | Creates a CDR export and downloads when ready. |
| [`EncryptionExample.cs`](EncryptionExample.cs) | `encryption` | Encrypts a file and uploads to `encrypted_files`. |
| [`WebhookExample.cs`](WebhookExample.cs) | `webhook` | Demonstrates webhook callback signature validation. |

## Troubleshooting

If `DIDWW_API_KEY` is missing, examples fail with:

```
DIDWW_API_KEY environment variable is not set
```

If you see `Unable to run your project. Your project targets multiple frameworks`, add `--framework net10.0` (or `net9.0`) to the command.
