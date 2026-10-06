# DIDWW API v3 .NET SDK

[![Tests](https://github.com/didww/didww-api-3-dotnet-sdk/actions/workflows/tests.yml/badge.svg?branch=main)](https://github.com/didww/didww-api-3-dotnet-sdk/actions/workflows/tests.yml)
![Coverage](https://didww.github.io/didww-api-3-dotnet-sdk/badge_combined.svg)

.NET client for [DIDWW API v3](https://doc.didww.com).

## About DIDWW API v3

The DIDWW API provides a simple yet powerful interface that allows you to fully integrate your own applications with DIDWW services. An extensive set of actions may be performed using this API, such as ordering and configuring phone numbers, setting capacity, creating SIP trunks and retrieving CDRs and other operational data.

The DIDWW API v3 is a fully compliant implementation of the [JSON API specification](http://jsonapi.org/format/).

This SDK uses [JsonApiSerializer](https://github.com/codecutout/JsonApiSerializer) for JSON:API serialization and deserialization.

Read more https://doc.didww.com/api

## API Version

This SDK sends the `X-DIDWW-API-Version: 2026-04-16` header with every request by default.

| NuGet Version | Branch | DIDWW API Version |
|---------------|--------|-------------------|
| **3.x** | [`main`](https://github.com/didww/didww-api-3-dotnet-sdk) | [`2026-04-16`](https://doc.didww.com/api3/2026-04-16/index.html) |
| **2.x** | [`2022-05-10`](https://github.com/didww/didww-api-3-dotnet-sdk/tree/2022-05-10) | [`2022-05-10`](https://doc.didww.com/api3/2022-05-10/index.html) |

## Requirements

- .NET 9.0 or later

## Installation

```bash
dotnet add package Didww.Api3
```

## Usage

```csharp
using Didww.Api3;

var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Production))
    .SetTimeout(TimeSpan.FromSeconds(30))
    .Build();

// Check balance
var balance = (await client.Balance().FindAsync()).Data;
Console.WriteLine($"Balance: {balance.BalanceAmount}, Credit: {balance.Credit}");

// List DID groups with stock keeping units
var queryParams = new QueryParams().Include("stock_keeping_units");
var didGroups = await client.DidGroups().ListAsync(queryParams);
```

For more examples visit [examples/](examples/Didww.Api3.Examples/).

For details on obtaining your API key please visit https://doc.didww.com/api3/configuration.html

## Configuration

```csharp
// Production
var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Production))
    .SetTimeout(TimeSpan.FromSeconds(30))
    .Build();

// Sandbox
var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Sandbox))
    .Build();
```

### Environments

| Environment | Base URL |
|---|---|
| `Production` | `https://api.didww.com/v3` |
| `Sandbox` | `https://sandbox-api.didww.com/v3` |

### Custom HTTP Handler

You can pass a custom `HttpMessageHandler` for advanced configuration such as proxy support:

```csharp
var handler = new HttpClientHandler
{
    Proxy = new WebProxy("http://proxy.example.com:8080"),
    UseProxy = true
};

var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Production))
    .SetInnerHandler(handler)
    .Build();
```

## Resources

### Read-Only Resources

```csharp
using Didww.Api3.Http;
using Didww.Api3.Resource;

// Countries
var countries = await client.Countries().ListAsync();
var country = (await client.Countries().FindAsync("uuid")).Data;

// Regions
var regions = await client.Regions().ListAsync();

// Cities
var cities = await client.Cities().ListAsync();

// Areas
var areas = await client.Areas().ListAsync();

// NANPA Prefixes
var prefixes = await client.NanpaPrefixes().ListAsync();

// POPs (Points of Presence)
var pops = await client.Pops().ListAsync();

// DID Group Types
var types = await client.DidGroupTypes().ListAsync();

// DID Groups (with stock keeping units)
var groups = await client.DidGroups().ListAsync(new QueryParams().Include("stock_keeping_units"));

// Available DIDs
var available = await client.AvailableDids().ListAsync();

// Proof Types
var proofTypes = await client.ProofTypes().ListAsync();

// Public Keys
var publicKeys = await client.PublicKeys().ListAsync();

// Address Requirements
var requirements = await client.AddressRequirements().ListAsync();

// Balance (singleton)
var balance = (await client.Balance().FindAsync()).Data;
```

### DIDs

```csharp
// List DIDs
var dids = await client.Dids().ListAsync();

// Update DID - assign trunk and capacity
var did = (await client.Dids().FindAsync("uuid")).Data;
did.Description = "Updated";
did.CapacityLimit = 20;
did.VoiceInTrunk = VoiceInTrunk.Build("trunk-uuid");
did = (await client.Dids().UpdateAsync(did)).Data;
```

### Voice In Trunks

```csharp
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;

// Create SIP trunk
var sipConfig = new SipConfiguration
{
    Username = "myuser",
    Host = "203.0.113.1",
    Port = 5060,
    CodecIds = new List<Codec> { Codec.PCMU, Codec.PCMA, Codec.G729 },
    TransportProtocolId = TransportProtocol.UDP,
    SstRefreshMethodId = SstRefreshMethod.INVITE,
    MediaEncryptionMode = MediaEncryptionMode.Disabled
};

var trunk = new VoiceInTrunk
{
    Name = "My SIP Trunk",
    Configuration = sipConfig
};

var trunkResponse = await client.VoiceInTrunks().CreateAsync(trunk);

// Create PSTN trunk
var pstnConfig = new PstnConfiguration { Dst = "558540420024" };
var pstnTrunk = new VoiceInTrunk
{
    Name = "My PSTN Trunk",
    Configuration = pstnConfig
};
await client.VoiceInTrunks().CreateAsync(pstnTrunk);

// Update trunk
var existingTrunk = VoiceInTrunk.Build("trunk-uuid");
existingTrunk.Name = "Renamed trunk";
await client.VoiceInTrunks().UpdateAsync(existingTrunk);

// Delete trunk
await client.VoiceInTrunks().DeleteAsync("trunk-uuid");
```

#### SIP Registration (2026-04-16)

A Voice In Trunk can also authenticate inbound calls via SIP registration
credentials generated by DIDWW. The SDK auto-cascades the dependent fields
the server requires:

* setting `EnabledSipRegistration = true` clears any previously-set
  `Host` / `Port` (the server rejects them with 422 otherwise);
* setting `Host` to a non-null value flips `EnabledSipRegistration` back
  to `false` and forces `UseDidInRuri = false` so the server accepts the
  disable PATCH.

The server generates `IncomingAuthUsername` and `IncomingAuthPassword` and
surfaces them in the response when SIP registration is enabled.

```csharp
var sipConfig = new SipConfiguration
{
    EnabledSipRegistration = true,
    UseDidInRuri           = true,
    CnamLookup             = true
};

var trunk = new VoiceInTrunk { Name = "Office (registered)", Configuration = sipConfig };
var response = await client.VoiceInTrunks().CreateAsync(trunk);
var created = (SipConfiguration)response.Data.Configuration!;
// created.IncomingAuthUsername — server-generated
// created.IncomingAuthPassword — server-generated
```

To disable SIP registration on an existing trunk, just set `Host` — the
cascade flips `EnabledSipRegistration` to `false` and `UseDidInRuri` to
`false` automatically:

```csharp
var disable = new SipConfiguration { Host = "sip.example.com" };
var existing = VoiceInTrunk.Build("trunk-uuid");
existing.Configuration = disable;
await client.VoiceInTrunks().UpdateAsync(existing);
```

### Voice In Trunk Groups

```csharp
var group = new VoiceInTrunkGroup
{
    Name = "Primary Group",
    CapacityLimit = 50
};
await client.VoiceInTrunkGroups().CreateAsync(group);
```

### Voice Out Trunks

Voice Out Trunks use a polymorphic `AuthenticationMethod` (2026-04-16). Three types are supported:

- **`CredentialsAndIpAuthenticationMethod`** -- default method; `Username` and `Password` are server-generated and returned in the response.
- **`TwilioAuthenticationMethod`** -- requires a `TwilioAccountSid`.
- **`IpOnlyAuthenticationMethod`** -- read-only; can only be configured by DIDWW staff upon request. Cannot be set via the API.

```csharp
using Didww.Api3.Resource.Configuration.AuthenticationMethod;

// NOTE: 203.0.113.0/24 is RFC 5737 TEST-NET-3 documentation space.
// Replace with the real CIDR of your SIP infrastructure.
var trunk = new VoiceOutTrunk
{
    Name = "My Outbound Trunk",
    OnCliMismatchAction = OnCliMismatchAction.ReplaceCli,
    DefaultDid = Did.Build("did-uuid"),
    AuthenticationMethod = new CredentialsAndIpAuthenticationMethod
    {
        AllowedSipIps = new List<string> { "203.0.113.0/24" },
    }
};
var response = await client.VoiceOutTrunks().CreateAsync(trunk);
// response.Data.AuthenticationMethod as CredentialsAndIpAuthenticationMethod -- Username/Password server-generated
```

### Orders

```csharp
using Didww.Api3.Resource.OrderItem;

// Order by SKU
var order = new Order
{
    AllowBackOrdering = true,
    Items = new List<OrderItemBase>
    {
        new DidOrderItem { SkuId = "sku-uuid", Qty = 2 }
    }
};
var response = await client.Orders().CreateAsync(order);

// Order available DID
var order = new Order
{
    Items = new List<OrderItemBase>
    {
        new AvailableDidOrderItem
        {
            SkuId = "sku-uuid",
            AvailableDidId = "available-did-uuid"
        }
    }
};

// Order capacity
var order = new Order
{
    Items = new List<OrderItemBase>
    {
        new CapacityOrderItem
        {
            CapacityPoolId = "pool-uuid",
            Qty = 1
        }
    }
};
```

### DID Reservations

```csharp
var reservation = new DidReservation();
reservation.Description = "Reserved for client";
reservation.AvailableDid = AvailableDid.Build("available-did-uuid");
await client.DidReservations().CreateAsync(reservation);

// Delete reservation
await client.DidReservations().DeleteAsync("reservation-uuid");
```

### Shared Capacity Groups

```csharp
var group = new SharedCapacityGroup();
group.Name = "Shared Group";
group.SharedChannelsCount = 20;
group.CapacityPool = CapacityPool.Build("pool-uuid");
await client.SharedCapacityGroups().CreateAsync(group);
```

### Identities

```csharp
var identity = new Identity
{
    FirstName = "John",
    LastName = "Doe",
    PhoneNumber = "12125551234",
    IdentityType = IdentityType.Personal,
    Country = Country.Build("country-uuid")
};
await client.Identities().CreateAsync(identity);
```

### Addresses

```csharp
var address = new Address
{
    CityName = "New York",
    PostalCode = "10001",
    AddressLine = "123 Main St",
    Identity = Identity.Build("identity-uuid"),
    Country = Country.Build("country-uuid")
};
await client.Addresses().CreateAsync(address);
```

### Exports

```csharp
var export = new Export
{
    ExportType = ExportType.CdrIn,
    Filters = new Dictionary<string, object>
    {
        { "from", "2026-04-01 00:00:00" },
        { "to", "2026-04-15 23:59:59" }
    }
};
var response = await client.Exports().CreateAsync(export);

// Download when completed
var completed = (await client.Exports().FindAsync(response.Data.Id)).Data;
if (completed.Url != null)
{
    await client.DownloadExportAsync(completed, "/tmp/export.csv");
}
```

### Address Verifications

```csharp
// List address verifications
var verifications = await client.AddressVerifications().ListAsync();

// Create address verification
var verification = new AddressVerification
{
    CallbackUrl = "https://example.com/callback",
    CallbackMethod = CallbackMethod.Post,
    Address = Address.Build("address-uuid"),
    Dids = new List<Did> { Did.Build("did-uuid") }
};
var result = await client.AddressVerifications().CreateAsync(verification);
```

### Emergency Services (2026-04-16)

```csharp
// List emergency requirements
var emergReqs = await client.EmergencyRequirements().ListAsync();

// Create emergency verification
var emergVerification = new EmergencyVerification
{
    CallbackUrl = "https://example.com/callback",
    CallbackMethod = CallbackMethod.Post,
    Address = Address.Build("address-uuid"),
    Dids = new List<Did> { Did.Build("did-uuid") }
};
var emergResult = await client.EmergencyVerifications().CreateAsync(emergVerification);

// List emergency calling services
var emergServices = await client.EmergencyCallingServices().ListAsync();
```

### DID History (2026-04-16)

```csharp
// List DID history
var history = await client.DidHistory().ListAsync();
foreach (var entry in history.Data)
{
    Console.WriteLine($"{entry.Action} {entry.CreatedAt}");
}
```

## Filtering, Sorting, and Pagination

```csharp
using Didww.Api3.Http;

var queryParams = new QueryParams()
    .Filter("country.id", "uuid")
    .Filter("name", "Arizona")
    .Include("country")
    .Sort("name")
    .Page(1, 25);

var regions = await client.Regions().ListAsync(queryParams);
```

## Dirty PATCH Serialization

The SDK tracks which fields have been modified and sends only those fields in PATCH requests. This avoids overwriting server-side values that your code hasn't touched.

### Updating a fetched resource

When you fetch a resource and modify it, only the changed fields are sent:

```csharp
var did = (await client.Dids().FindAsync("uuid")).Data;
did.Description = "Updated description";
// PATCH payload includes only "description", not all attributes
did = (await client.Dids().UpdateAsync(did)).Data;
```

### Building a resource for update

Use `Build(id)` to create a lightweight resource for PATCH without fetching first:

```csharp
var trunk = VoiceInTrunk.Build("trunk-uuid");
trunk.Name = "New name";
// PATCH payload includes only "name"
await client.VoiceInTrunks().UpdateAsync(trunk);
```

### Clearing a field with explicit null

Setting a property to `null` marks it as dirty and includes an explicit `null` in the payload, which clears the server-side value:

```csharp
var did = Did.Build("uuid");
did.Description = null;
// PATCH payload includes "description": null
did = (await client.Dids().UpdateAsync(did)).Data;
```

### Clearing a relationship

Setting a relationship to `null` sends `"data": null` for to-one relationships:

```csharp
var did = Did.Build("uuid");
did.VoiceInTrunk = null;
// PATCH payload includes: "relationships": { "voice_in_trunk": { "data": null } }
did = (await client.Dids().UpdateAsync(did)).Data;
```

### Included resources

Dirty tracking is automatically enabled on included (sideloaded) resources, so you can fetch with includes and update a related resource directly:

```csharp
var qp = new QueryParams().Include("voice_in_trunk");
var did = (await client.Dids().FindAsync("uuid", qp)).Data;
var trunk = did.VoiceInTrunk;
trunk.Name = "Renamed trunk";
// PATCH payload includes only "name"
await client.VoiceInTrunks().UpdateAsync(trunk);
```

## Error Handling

```csharp
using Didww.Api3.Exception;

try
{
    await client.VoiceInTrunks().FindAsync("nonexistent");
}
catch (DidwwApiException e)
{
    Console.WriteLine($"HTTP Status: {e.HttpStatus}");
    foreach (var error in e.Errors)
    {
        Console.WriteLine($"Error: {error.Detail}");
    }
}
catch (DidwwClientException e)
{
    Console.WriteLine($"Client error: {e.Message}");
}
```

## File Encryption

The SDK provides an `Encrypt` utility for encrypting files before upload, using RSA-OAEP + AES-256-CBC (matching DIDWW's encryption requirements).

```csharp
var encrypt = new Encrypt(client);

byte[] fileData = File.ReadAllBytes("document.pdf");
byte[] encryptedData = encrypt.EncryptData(fileData);

var fileIds = await client.UploadEncryptedFileAsync(
    encryptedData,
    "document.pdf",
    encrypt.Fingerprint,
    "My document"
);
```

## Webhook Signature Validation

Validate incoming webhook callbacks from DIDWW using HMAC-SHA1 signature verification.

Initialize the validator with the callback secret that is enabled in the DIDWW User Panel (**APIs → DIDWW API 3 → Callback Secrets**). DIDWW signs every callback with it and sends callbacks only while a callback secret is enabled.

```csharp
using Didww.Api3.Callback;

var validator = new RequestValidator("your-callback-secret");
var isValid = validator.Validate(
    requestUrl,       // full original URL
    payloadParams,    // Dictionary<string, string> of payload key-value pairs
    signature         // value of X-DIDWW-Signature header
);
```

## Trunk Configuration Types

| Type | Class |
|---|---|
| SIP | `SipConfiguration` |
| PSTN | `PstnConfiguration` |

## Order Item Types

| Type | Class |
|---|---|
| DID | `DidOrderItem` |
| Capacity | `CapacityOrderItem` |
| Emergency | `EmergencyOrderItem` |
| Generic | `GenericOrderItem` |

## All Supported Resources

| Resource | Class | Operations |
|---|---|---|
| Country | `Country` | List, Find |
| Region | `Region` | List, Find |
| City | `City` | List, Find |
| Area | `Area` | List, Find |
| NanpaPrefix | `NanpaPrefix` | List, Find |
| Pop | `Pop` | List, Find |
| DidGroupType | `DidGroupType` | List, Find |
| DidGroup | `DidGroup` | List, Find |
| AvailableDid | `AvailableDid` | List, Find |
| ProofType | `ProofType` | List, Find |
| PublicKey | `PublicKey` | List, Find |
| AddressRequirement | `AddressRequirement` | List, Find |
| SupportingDocumentTemplate | `SupportingDocumentTemplate` | List, Find |
| Balance | `Balance` | Find |
| Did | `Did` | List, Find, Update, Delete |
| VoiceInTrunk | `VoiceInTrunk` | List, Find, Create, Update, Delete |
| VoiceInTrunkGroup | `VoiceInTrunkGroup` | List, Find, Create, Update, Delete |
| VoiceOutTrunk | `VoiceOutTrunk` | List, Find, Create, Update, Delete |
| VoiceOutTrunkRegenerateCredential | `VoiceOutTrunkRegenerateCredential` | Create |
| DidReservation | `DidReservation` | List, Find, Create, Delete |
| CapacityPool | `CapacityPool` | List, Find |
| SharedCapacityGroup | `SharedCapacityGroup` | List, Find, Create, Update, Delete |
| Order | `Order` | List, Find, Create |
| Export | `Export` | List, Find, Create, Update |
| Address | `Address` | List, Find, Create, Delete |
| AddressVerification | `AddressVerification` | List, Create, Update |
| Identity | `Identity` | List, Find, Create, Delete |
| EncryptedFile | `EncryptedFile` | List, Find, Delete |
| PermanentSupportingDocument | `PermanentSupportingDocument` | Create, Delete |
| Proof | `Proof` | Create, Delete |
| AddressRequirementValidation | `AddressRequirementValidation` | Create |
| DidHistory | `DidHistory` | List |
| EmergencyRequirement | `EmergencyRequirement` | List, Find |
| EmergencyRequirementValidation | `EmergencyRequirementValidation` | Create |
| EmergencyCallingService | `EmergencyCallingService` | List, Find, Delete |
| EmergencyVerification | `EmergencyVerification` | List, Find, Create, Update |

## Date and Datetime Fields

The SDK distinguishes between date-only and datetime fields:

- **Datetime fields** are deserialized as `DateTimeOffset?`:
  - `CreatedAt` — present on most resources
  - `ExpiresAt` — `Did`, `DidReservation`, `Proof`, `EncryptedFile` (nullable)
  - `ActivatedAt` — `EmergencyCallingService` (nullable)
  - `CanceledAt` — `EmergencyCallingService` (nullable)
- **Date-only fields** (`Identity.BirthDate`) are deserialized as `DateOnly?`.
- **Date-only fields kept as strings** remain as `string?`:
  - `CapacityPool.RenewDate`, `EmergencyCallingService.RenewDate` — `"YYYY-MM-DD"` (nullable)
  - `DidOrderItem.BilledFrom`, `DidOrderItem.BilledTo`
- **String fields** (not numeric):
  - `EmergencyRequirement.EstimateSetupTime` — e.g. `"7-14 days"`, `"1"`
  - `EmergencyRequirement.RequirementRestrictionMessage` — nullable

**Important changes from previous API versions:**
- `ExpireAt` renamed to `ExpiresAt` on `DidReservation` and `EncryptedFile`
- `RenewDate` is a date-only string, NOT a `DateTimeOffset`
- `EstimateSetupTime` is a string, NOT an integer

```csharp
var did = (await client.Dids().FindAsync("uuid")).Data;
Console.WriteLine(did.CreatedAt);   // 2024-01-15T10:00:00+00:00
Console.WriteLine(did.ExpiresAt);   // null or 2025-01-15T10:00:00+00:00

var identity = (await client.Identities().FindAsync("uuid")).Data;
Console.WriteLine(identity.BirthDate);  // 1990-05-20
```

## Enums

The SDK provides enum types in `Didww.Api3.Resource.Enums`:

`CallbackMethod`, `IdentityType`, `OrderStatus`, `ExportType`, `ExportStatus`, `CliFormat`,
`OnCliMismatchAction`\*, `MediaEncryptionMode`, `DefaultDstAction`, `VoiceOutTrunkStatus`,
`EmergencyCallingServiceStatus`, `EmergencyVerificationStatus`, `DiversionRelayPolicy`,
`TransportProtocol`, `Codec`, `RxDtmfFormat`, `TxDtmfFormat`, `SstRefreshMethod`,
`ReroutingDisconnectCode`, `Feature`, `AreaLevel`, `AddressVerificationStatus`, `StirShakenMode`,
`DidHistoryAction`, `DidHistoryMethod`

\* `ReplaceCli` and `RandomizeCli` require additional account configuration. Contact DIDWW support to enable these values.

## Development

```bash
# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Check formatting
dotnet format --verify-no-changes

# Build
dotnet build
```

## Release

To publish a new version to NuGet:

1. Update the version in `src/Didww.Api3/Didww.Api3.csproj`
2. Commit and push to `main`
3. Create and push a version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

The [release workflow](.github/workflows/release.yml) will automatically build, test, and publish the package to [NuGet](https://www.nuget.org/).

## Contributing

Bug reports and pull requests are welcome on GitHub at https://github.com/didww/didww-api-3-dotnet-sdk

## License

The package is available as open source under the terms of the [MIT License](https://opensource.org/licenses/MIT).
