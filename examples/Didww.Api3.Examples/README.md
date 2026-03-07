# Examples

All examples read the API key from the `DIDWW_API_KEY` environment variable.

## Prerequisites

- .NET 9.0+ or .NET 10.0+
- DIDWW API key for sandbox account

## Environment variables

- `DIDWW_API_KEY` (required): your DIDWW API key

## Run an example

```bash
# Run all examples
DIDWW_API_KEY=your_api_key dotnet run --project examples/Didww.Api3.Examples

# Run a specific example
DIDWW_API_KEY=your_api_key dotnet run --project examples/Didww.Api3.Examples -- balance
```

## Available examples

| Example | Description |
|---|---|
| [`BalanceExample.cs`](BalanceExample.cs) | Fetches and prints current account balance and credit. |
| [`CountriesExample.cs`](CountriesExample.cs) | Lists countries, demonstrates filtering, and fetches one country by ID. |
| [`DidsExample.cs`](DidsExample.cs) | Lists DIDs and demonstrates DID updates. |
| [`TrunksExample.cs`](TrunksExample.cs) | Creates SIP and PSTN trunks with enum-based configuration. |
| [`OrdersExample.cs`](OrdersExample.cs) | Creates a DID order by SKU. |
| [`ExportsExample.cs`](ExportsExample.cs) | Creates a CDR export and downloads it when ready. |
| [`EncryptionExample.cs`](EncryptionExample.cs) | Encrypts a file and uploads to `encrypted_files`. |
| [`WebhookExample.cs`](WebhookExample.cs) | Demonstrates webhook callback signature validation. |

## Troubleshooting

If `DIDWW_API_KEY` is missing, examples fail with:

```
DIDWW_API_KEY environment variable is not set
```
