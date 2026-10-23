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

This SDK targets DIDWW API version `2026-10-22` (`DidwwClient.DefaultApiVersion`). The version is not sent as a header: it is requested in the `api_version` of every access token the SDK mints (see [Authentication](#authentication)).

| NuGet Version | Branch | DIDWW API Version |
|---------------|--------|-------------------|
| **4.x** | [`main`](https://github.com/didww/didww-api-3-dotnet-sdk) | [`2026-10-22`](https://doc.didww.com/api3/2026-10-22/index.html) |
| **3.x** | [`release-3`](https://github.com/didww/didww-api-3-dotnet-sdk/tree/release-3) | [`2026-04-16`](https://doc.didww.com/api3/2026-04-16/index.html) |
| **2.x** | [`2022-05-10`](https://github.com/didww/didww-api-3-dotnet-sdk/tree/2022-05-10) | [`2022-05-10`](https://doc.didww.com/api3/2022-05-10/index.html) |

`SetApiVersion` requests a later version. `Build()` throws an `ArgumentException` for a value that is not a `YYYY-MM-DD` date and for a version earlier than `2026-10-22`. The resources of earlier API versions are not in this package: use the 3.x line from the [`release-3`](https://github.com/didww/didww-api-3-dotnet-sdk/tree/release-3) branch for API `2026-04-16`.

## Requirements

- .NET 9.0 or later

## Installation

```bash
dotnet add package Didww.Api3 --version 4.0.0
```

## Usage

```csharp
using Didww.Api3;
using Didww.Api3.Http;

var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Production))
    .Build();

// Check balance
var balance = (await client.Balance().FindAsync()).Data;
Console.WriteLine($"Balance: {balance.BalanceAmount}, Credit: {balance.Credit}");

// List DID groups with stock keeping units
var queryParams = new QueryParams().Include("stock_keeping_units");
var didGroups = await client.DidGroups().ListAsync(queryParams);
```

`DidwwClient` is safe for concurrent use: build one and reuse it, for example as a singleton. Every async method takes an optional `CancellationToken` as its last parameter.

For more examples visit [examples/](examples/Didww.Api3.Examples/).

For details on obtaining your API key please visit https://doc.didww.com/api3/configuration.html

## Configuration

```csharp
// Production
var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Production))
    .Build();

// Sandbox, read-only tokens, logging through Microsoft.Extensions.Logging
var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Sandbox))
    .SetScope(DidwwScope.Read)
    .SetLogger(loggerFactory.CreateLogger("Didww"))
    .Build();
```

| Builder method | Default | Description |
|----------------|---------|-------------|
| `SetCredentials(credentials)` | required | `new DidwwCredentials(apiKey, environment)`, with `DidwwEnvironment.Production` or `DidwwEnvironment.Sandbox`. The environment selects the API, token service and price hosts. See [Environments](#environments). |
| `SetApiVersion(version)` | `DidwwClient.DefaultApiVersion` | API version requested in every token. See [API Version](#api-version). |
| `SetScope(scope)` | none | `DidwwScope.Read` or `DidwwScope.Full`. Without it no scope is requested and the token service grants `full`. See [Scope](#scope). |
| `SetTokenStore(store)` | a new `MemoryTokenStore` per client | Cache for access tokens. See [Token cache](#token-cache). |
| `SetLogger(logger)` | `NullLogger.Instance` | `ILogger` for the token warnings and debug events. The default and `null` log nothing. See [Logging](#logging). |
| `SetInnerHandler(handler)` | a handler shared by the SDK | `HttpMessageHandler` used for every call. See [Custom HTTP Handler](#custom-http-handler-proxy-tls-etc). |
| `SetTimeout(timeout)` | 60 s | Time allowed until the response headers arrive. See [Custom HTTP Handler](#custom-http-handler-proxy-tls-etc). |
| `SetBaseUrl(url)` | API URL of the environment | JSON:API base URL only, including `/v3`. |
| `SetAuthUrl(url)` | Token service of the environment | Token service base URL. |
| `SetVoiceInPricesUrl(url)`, `SetVoiceOutPricesUrl(url)`, `SetSmsPricesUrl(url)` | Price hosts of the environment | Price service base URLs, with or without the `/v3` suffix. |

The API key is sent only to the token service. An overridden URL, for example to send the traffic through a proxy, does not change the token audiences (see [Authentication](#authentication)). `Build()` throws an `ArgumentNullException` without credentials, and an `ArgumentException` for an empty API key or one with a character outside printable ASCII (a control character other than tab, such as a trailing newline, or a non-ASCII character, such as a non-breaking space), an invalid API version, an undefined scope or environment value, or a URL that is not an absolute http or https URL.

### Environments

| Environment | API | Token service | Voice IN prices | Voice OUT prices | SMS prices |
|-------------|-----|---------------|-----------------|------------------|------------|
| `Production` | `https://api.didww.com/v3` | `https://auth.didww.com` | `https://api-vin.didww.com/v3` | `https://api-vout.didww.com/v3` | `https://api-sms.didww.com/v3` |
| `Sandbox` | `https://sandbox-api.didww.com/v3` | `https://auth-sandbox.didww.com` | `https://api-vin-sandbox.didww.com/v3` | `https://api-vout-sandbox.didww.com/v3` | `https://api-sms-sandbox.didww.com/v3` |

### Custom HTTP Handler (Proxy, TLS, etc.)

You can pass a custom `HttpMessageHandler` for advanced configuration such as proxy support:

```csharp
using System.Net;

var handler = new HttpClientHandler
{
    Proxy = new WebProxy("http://proxy.example.com:8080"),
    UseProxy = true,
    AllowAutoRedirect = false
};

var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Production))
    .SetInnerHandler(handler)
    .Build();
```

The SDK adds the bearer token to every request; the API key goes only to `POST /token` on the token service. The handler is used as given for every call: the token service, the JSON:API, export download, encrypted file upload and prices. The SDK never follows redirects, so set `AllowAutoRedirect = false` on your handler (`HttpClientHandler` and `SocketsHttpHandler` follow redirects by default): a redirect your handler follows anyway is reported as an error by the token request, the export download and the prices calls, but only afterwards: by then the request, including the API key of a token request, has already reached the redirect target. Leave `AutomaticDecompression` off, so that price and export files are written exactly as served. The SDK does not dispose your handler.

Without `SetInnerHandler` the SDK uses one `SocketsHttpHandler`, shared by every client of the process, that follows no redirects, does not decompress, neither stores nor sends cookies (so no state is carried between clients or API keys), limits connecting (TCP and TLS) to 10 s and renews pooled connections after 15 minutes.

`SetTimeout` (default 60 s) bounds the time until the response headers arrive. JSON:API answers, price lookups and uploads are read within it too; price and export files are streamed after it, so a large file is not cut off by it. Pass a `CancellationToken` to bound a whole download. Each attempt of a token request is limited to 10 s, independently of `SetTimeout`.

## Authentication

Every request carries `Authorization: Bearer <token>`. The SDK mints the tokens itself with `POST /token` on the token service, sending your API key in the `Api-Key` header. The API key is sent nowhere else. No code change is needed: `new DidwwCredentials("your-api-key", environment)` works as before.

- **Audience.** One token per audience, minted on the first call to a service and reused until it expires. The audience is the production hostname of the service, in the sandbox too:

  | Calls | Token audience |
  |-------|----------------|
  | JSON:API resources, export download, encrypted file upload | `api.didww.com` |
  | Voice IN prices | `api-vin.didww.com` |
  | Voice OUT prices | `api-vout.didww.com` |
  | SMS OUT (A2P, P2P, OTP) and SMS IN prices | `api-sms.didww.com` |

  `client.PublicKeys().ListAsync()` is sent without a token.
- **Expiry and 401.** A cached token lives for the `expires_in` granted; the SDK adds no local expiry margin. When a request is answered 401, the SDK drops that token, mints a new one and sends the request once more. A second 401 drops the new token too and is not retried: it becomes the error of the call (see [Error Handling](#error-handling)). A 403, such as `insufficient_scope`, is never retried.
- **Concurrency.** Concurrent calls of one `DidwwClient` that need the same token wait for a single mint. A caller whose `CancellationToken` is cancelled stops waiting; the mint itself goes on for the other callers.
- **Token service errors** are thrown as `DidwwAuthException` from any call that needs a new token (kinds in [Error Handling](#error-handling)):
  - A 429 throws `DidwwAuthErrorKind.RateLimited` with `RetryAfter` (the `Retry-After` seconds, at least 1 s). Every further mint for the same token service and API key in the process fails the same way without a request until that time has passed, plus a small random delay so that clients do not retry in step.
  - A 5xx answer, a connection failure or a timeout is retried with exponential backoff and full jitter (0.5 s base, 8 s cap, 4 attempts in all), then throws `ServerError` or `ConnectionError`.
  - A 400, a 401 or a TLS failure is thrown at once, without a retry.

### Scope

```csharp
using Didww.Api3.Auth;

var readOnly = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Production))
    .SetScope(DidwwScope.Read) // GET and HEAD only; DidwwScope.Full allows every method
    .Build();
// Without SetScope the scope is not sent and the token service grants full.
```

A `Read` token is enough for the price lookups and price files. A write with a `Read` token is answered 403, which the JSON:API repositories throw as a `DidwwApiException` (see [Error Handling](#error-handling)).

The SDK compares each token it receives with what it asked for:

- a different `scope` (for example `full` granted for a `read` request) or a different `api_version` is logged as a warning through the logger (see [Logging](#logging)), and the token is used;
- a token with an empty access token, or whose `aud` does not include the requested audience, throws `DidwwAuthErrorKind.InvalidGrant` and is not cached.

### Token cache

Tokens are cached in an `ITokenStore`, keyed by the token service URL, the API key (as a SHA-256 hash), the audience, the API version and the scope. The key is an opaque string starting with `didww:token:v1:`. Each entry is stored for the token's `expires_in`.

The default `MemoryTokenStore` keeps the tokens in process memory. Each client built without `SetTokenStore` has its own, so build one `DidwwClient` and reuse it. To share tokens between clients or processes, pass one store to all of them, for example your own implementation backed by Redis:

```csharp
public interface ITokenStore
{
    ValueTask<DidwwToken?> GetAsync(string key, CancellationToken cancellationToken);
    ValueTask SetAsync(string key, DidwwToken token, TimeSpan ttl, CancellationToken cancellationToken);
    ValueTask DeleteAsync(string key, CancellationToken cancellationToken);
}

var client = DidwwClient.NewBuilder()
    .SetCredentials(new DidwwCredentials("your-api-key", DidwwEnvironment.Production))
    .SetTokenStore(myRedisStore)
    .Build();
```

`DidwwToken` serializes with all its fields under their wire names, with Newtonsoft.Json and System.Text.Json, so that a store can persist it; its `ToString` and debugger display never show the access token. A shared store holds live bearer tokens, so protect it like the API key. Processes that share a store can each mint once when a token is missing.

A store that throws does not fail the call: the error is logged as a warning, and the call goes on with a newly minted token (a failed write leaves the token uncached). Every store call gets a `CancellationToken`. The calls the SDK makes on its own behalf, re-reading the store before a mint, writing a minted token, and the read and delete that evict a refused token after a 401, are limited to 10 seconds, so a store should honour its token. The first read of a call follows the caller's token.

### Logging

`SetLogger` sets the `Microsoft.Extensions.Logging` `ILogger` for token events. Following the .NET library convention, the default is `NullLogger.Instance`: nothing is logged, not even the mismatch warnings, until you pass a logger.

- Warnings: `DIDWW token scope mismatch` and `DIDWW token api_version mismatch` (with `Requested` and `Granted`), and token store failures (`DIDWW token store read failed`, `write failed`, `write skipped`, `delete failed`).
- Debug: `DIDWW token minted`, with `Audience`, `Scope`, `ApiVersion`, `ExpiresIn` and `Duration`.

Neither the token nor the API key is ever logged.

### SDK design decisions

- **Audience strategy.** One token per audience, minted on first use, rather than one token naming every service. An account that never calls the prices services never requests their audiences, so a refused audience cannot break DID management, and every token is valid for one service only.
- **Cache store.** In-process memory by default, because a .NET service usually keeps one `DidwwClient` for its lifetime. A shared store is opt-in through `SetTokenStore`. Lifetime is decided by the server through the 401 refresh, not by a local expiry margin.
- **Gzip handling.** Price and export files are streamed to your `Stream` or file exactly as served, still gzipped, without being held in memory. The SDK never decompresses a price file on its own: `PriceFileReader` reads a downloaded file when you want rows, and `DownloadAndDecompressExportAsync` writes an export as plain CSV.
- **Upload retry.** The 401 retry sends the request body a second time. `UploadEncryptedFileAsync` takes the encrypted bytes as a `byte[]` and builds the multipart body in memory, so the retry always resends the complete body; there is no streamed-upload limitation.

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

// DID Groups (with stock keeping units), filtered by number type
var groups = await client.DidGroups().ListAsync(new QueryParams()
    .Include("stock_keeping_units")
    .Filter("number_type", "toll_free"));

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

`DidGroup`, `AddressRequirement`, `EmergencyRequirement` and `EmergencyCallingService` carry a `NumberType`, one of the `NumberType` values (`Local`, `National`, `Mobile`, `TollFree`, `SharedCost`, `Global`). `DidGroup.IsDemo` flags demo groups. `NumberType` replaces the `DidGroupType` resource and the `did_group_type` relationship, which API `2026-10-22` no longer has. The `pops` resource is gone too: a Voice IN trunk carries its point of presence in the `Pop` attribute (see [Voice In Trunks](#voice-in-trunks)).

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

#### SMS routing

A DID's inbound messages go to an SMS trunk or to an SMS trunk group. The two are exclusive: assigning one sends the other as `null` in the same request, as `VoiceInTrunk` and `VoiceInTrunkGroup` do. Setting one of them to `null` sends only that one as `null`; set both to unroute the DID.

```csharp
// sends sms_trunk and sms_trunk_group: null
var route = Did.Build("did-uuid");
route.SmsTrunk = SmsTrunk.Build("sms-trunk-uuid");
await client.Dids().UpdateAsync(route);

// sends sms_trunk_group and sms_trunk: null
var toGroup = Did.Build("did-uuid");
toGroup.SmsTrunkGroup = SmsTrunkGroup.Build("sms-trunk-group-uuid");
await client.Dids().UpdateAsync(toGroup);

// unroute: sends sms_trunk: null and sms_trunk_group: null
var unroute = Did.Build("did-uuid");
unroute.SmsTrunk = null;
unroute.SmsTrunkGroup = null;
await client.Dids().UpdateAsync(unroute);

var routed = await client.Dids().ListAsync(new QueryParams()
    .Filter("sms_trunk.id", "sms-trunk-uuid")
    .Include("sms_trunk", "sms_trunk_group")); // did.SmsTrunk, did.SmsTrunkGroup
```

An SMS trunk cannot be deleted while DIDs are routed to it, nor an SMS trunk group while DIDs are routed to it or to one of its trunks: unroute those DIDs, or route them elsewhere, first.

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

`Pop` is a plain attribute, one of the `Pop` values (`Nyc`, `Fra`, `Lac`, `Mia`, `Sg`, `Hk`, `Ams`). Leave it `null` to let DIDWW choose; setting it to `null` on a built or fetched trunk sends `"pop": null`. It is no longer a relationship and cannot be included.

```csharp
var popUpdate = VoiceInTrunk.Build("trunk-uuid");
popUpdate.Pop = Pop.Ams;
await client.VoiceInTrunks().UpdateAsync(popUpdate);
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

### SMS Trunks

An SMS trunk delivers inbound messages, accepts outbound ones, or both. Its `Configuration` is one of five types in namespace `Didww.Api3.Resource.Configuration.SmsTrunkConfiguration`:

| Type | Class | Notes |
|------|-------|-------|
| `sms_to_email` | `SmsToEmailConfiguration` | Inbound messages are forwarded as email. `SendTo`, `Subject` and `Message` are required. |
| `http_in` | `HttpInConfiguration` | Inbound messages are sent as an HTTP request to your `Url`. `HttpMethod` is one of the `SmsHttpMethod` values (`Get`, `Post`, `Put`); post and put also need `BodyType` (an `SmsBodyType` value) and `Body`. |
| `http_out` | `HttpOutConfiguration` | You submit outbound messages over HTTP. `AuthName` and `AuthPassword` are generated. The submit URL is `trunk.Host` (from the trunk's meta). |
| `smpp_esme` | `SmppEsmeConfiguration` | You bind to DIDWW as an SMPP ESME. `ServerLocation` is required; `SystemId` and `Password` are generated. |
| `smpp_smsc` | `SmppSmscConfiguration` | DIDWW binds to your SMSC with your `Host`, `SystemId`, `Password` and `ServerLocation`. `TxPort` defaults to 2775 and `RxPort` to 2776. |

The read-only `AllowIn` and `AllowOut` tell which directions a trunk handles. `ServerLocation` is `SmppServerLocations.Nyc` or `SmppServerLocations.Ams`; the sandbox accepts `nyc` only. A configuration type this SDK does not know is read as a `GenericSmsTrunkConfiguration` and sent back unchanged; its numbers keep their exact value.

```csharp
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using Didww.Api3.Resource.Enums;
using Newtonsoft.Json.Linq;

var trunk = (await client.SmsTrunks().CreateAsync(new SmsTrunk
{
    Name = "Inbound webhook",
    Configuration = new HttpInConfiguration
    {
        Url = "https://example.com/sms",
        HttpMethod = SmsHttpMethod.Post,
        BodyType = SmsBodyType.Json,
        Body = "{\"text\":\"{SMS_TEXT}\",\"from\":\"{SMS_SRC_ADDR}\",\"to\":\"{SMS_DST_ADDR}\"}"
    }
})).Data;

// On update the configuration type must stay the same; attributes left out keep their values.
var update = SmsTrunk.Build(trunk.Id!);
update.Configuration = new HttpInConfiguration { Url = "https://example.com/sms/v2" };
await client.SmsTrunks().UpdateAsync(update);

// Nullable attributes are cleared with a generic configuration of the same type: null is sent as null
var clear = SmsTrunk.Build(trunk.Id!);
clear.Configuration = new GenericSmsTrunkConfiguration("http_in", new JObject
{
    ["http_method"] = "get",
    ["body_type"] = JValue.CreateNull(),
    ["body"] = JValue.CreateNull()
});
await client.SmsTrunks().UpdateAsync(clear);

// List by configuration type, with the group
var trunks = await client.SmsTrunks().ListAsync(new QueryParams()
    .Filter("configuration.type", "http_in,sms_to_email")
    .Include("sms_trunk_group"));
```

Changes made inside a configuration object are not tracked: assign `Configuration` to send it in the next update. The configuration is sent as a whole, and a `null` member is left out, so on update it keeps its stored value. A typed configuration therefore cannot clear a nullable attribute: the `QueryParameters`, `Headers`, `BodyType` and `Body` of `http_in`, the `CallbackUrl` and `AllowedIps` of `http_out`, the `SystemType` and `AllowedIps` of `smpp_esme` and the `SystemType` of `smpp_smsc`. Send such a change as a `GenericSmsTrunkConfiguration` of the same type, as above: its `JValue.CreateNull()` values are sent as `null`.

`OutgoingDids` replaces the whole `outgoing_dids` list of the trunk: list every DID to keep (include `outgoing_dids` to read the current list), or assign an empty list to clear it.

The generated credentials of `http_out` and `smpp_esme` trunks are read-only and are never sent in a request. `ToString` masks them, the `Password` of an `SmppSmscConfiguration`, and the `Headers` of an `HttpInConfiguration`, which may carry the credentials of your endpoint. A `GenericSmsTrunkConfiguration` masks every attribute value, since its fields are not known; only the attribute names and explicit nulls are shown. The properties keep the real values. To rotate the generated credentials:

```csharp
await client.SmsTrunkRegenerateCredentials().CreateAsync(
    new SmsTrunkRegenerateCredential { SmsTrunk = SmsTrunk.Build(outTrunkId) });

// The response carries no credentials: read the trunk again
var outTrunk = (await client.SmsTrunks().FindAsync(outTrunkId)).Data;
switch (outTrunk.Configuration)
{
    case HttpOutConfiguration http:
        // http.AuthName and http.AuthPassword are the new credentials
        break;
    case SmppEsmeConfiguration esme:
        // esme.SystemId and esme.Password are the new credentials
        break;
}
```

### SMS Trunk Groups

An SMS trunk group spreads a DID's inbound messages over its trunks by their `Priority` (the higher value first). `http_out` trunks cannot join a group. Deleting a group also deletes the trunks in it.

```csharp
var group = (await client.SmsTrunkGroups().CreateAsync(new SmsTrunkGroup
{
    Name = "Inbound SMS",
    SmsTrunks = new List<SmsTrunk> { SmsTrunk.Build("sms-trunk-uuid") }
})).Data;
Console.WriteLine(group.SmsTrunksCount); // 1
```

`SmsTrunks` replaces the whole membership of the group: list every trunk to keep. Build the list from the included `sms_trunks`:

```csharp
var current = (await client.SmsTrunkGroups().FindAsync(group.Id!, new QueryParams().Include("sms_trunks"))).Data;
var update = SmsTrunkGroup.Build(group.Id!);
update.SmsTrunks = (current.SmsTrunks ?? new List<SmsTrunk>())
    .Select(t => SmsTrunk.Build(t.Id!))
    .Append(SmsTrunk.Build("another-sms-trunk-uuid"))
    .ToList();
await client.SmsTrunkGroups().UpdateAsync(update);

// An empty list removes every trunk from the group
var empty = SmsTrunkGroup.Build(group.Id!);
empty.SmsTrunks = new List<SmsTrunk>();
await client.SmsTrunkGroups().UpdateAsync(empty);
```

### Sender ID Verifications

A Sender ID verification covers the sender IDs your outbound messages use. It is created and reviewed outside the API: the API lists and reads verifications, and changes only their `Name` and the SMS trunk their messages are routed through. A closed verification cannot be changed. `Status` is one of the `SenderIdVerificationStatus` values and `SenderType` one of the `SenderType` values; `IsActive`, `IsClosed` and the other `Is*` properties test the status. `Meta` carries `setup_price` and `monthly_price` as decimal strings.

```csharp
var verifications = (await client.SenderIdVerifications().ListAsync(new QueryParams()
    .Filter("status", "active")
    .Include("sms_trunk"))).Data;

// http_out, smpp_esme or smpp_smsc trunks only
var route = SenderIdVerification.Build(verifications[0].Id!);
route.SmsTrunk = SmsTrunk.Build("sms-trunk-uuid");
await client.SenderIdVerifications().UpdateAsync(route);

// Outbound messages stop until a trunk is assigned again
var unroute = SenderIdVerification.Build(verifications[0].Id!);
unroute.SmsTrunk = null;
await client.SenderIdVerifications().UpdateAsync(unroute);
```

Deleting an SMS trunk leaves the verifications routed through it without a trunk.

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

`CompanyWebsite` belongs to business identities; a personal identity refuses it, and it is left out of a create request while `null`.

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

// SMS export
var smsOut = (await client.Exports().CreateAsync(new Export
{
    ExportType = ExportType.SmsOut,
    Filters = new Dictionary<string, object>
    {
        { "from", "2026-10-01 00:00:00" }, // inclusive
        { "to", "2026-10-02 00:00:00" },   // exclusive
        { "service_type", "a2p" },
        { "sms_trunk.id", new List<string> { "sms-trunk-uuid" } }
    }
})).Data;

// The export is pending until its file is generated; read it again later
var completed = (await client.Exports().FindAsync(smsOut.Id!)).Data;
if (completed.Url != null)
{
    await client.DownloadExportAsync(completed, "/tmp/export.csv.gz");           // gzipped CSV, as served
    await client.DownloadAndDecompressExportAsync(completed, "/tmp/export.csv");  // plain CSV

    await using var stream = File.Create("/tmp/copy.csv.gz");
    await client.DownloadExportAsync(completed.Url, stream);                     // any writable Stream
}
```

The export downloads send the token only to the origin (scheme, host and port) of the JSON:API base URL or of the environment, so export URLs, which the API builds on the environment's host, are still downloaded when `SetBaseUrl` points elsewhere; that request goes directly to the environment's host. A URL on any other origin throws a `DidwwClientException` and nothing is sent. Only a 200 answer is written, and redirects are not followed (see [Error Handling](#error-handling)).

`from` and `to` are required for every export type (`cdr_in`, `cdr_out`, `sms_in`, `sms_out`). The other filter keys depend on the type:

- `sms_in` and `sms_out`: `source_address_eq`, `source_address_cont`, `destination_address_eq`, `destination_address_cont`, `status`, `sms_id`, `sms_trunk.id` (a string or a list of strings);
- `sms_in` only: `trunk_type`, one or more of the `ExportTrunkTypes` values;
- `sms_out` only: `service_type` (`a2p` or `p2p`) and `sender_id_verification.id` (a string or a list of strings).

The doc comment of `Export.Filters` lists the keys of every type.

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

## Prices

Prices are served by three services outside the JSON:API (hosts in [Environments](#environments)). Each price list has a price file and a lookup:

| Accessor | Service | Price file | Lookup | Lookup rows |
|----------|---------|------------|--------|-------------|
| `client.VoiceInPrices()` | Voice IN | `prices.csv.gz` | `lookup/prices` | `VoiceInPrice` |
| `client.VoiceOutPrices()` | Voice OUT | `prices.csv.gz` | `lookup/prices` | `VoiceOutPrice` |
| `client.SmsOutA2PPrices()` | SMS | `sms_out/a2p/prices.csv.gz` | `sms_out/a2p/lookup/prices` | `SmsOutPrice` |
| `client.SmsOutP2PPrices()` | SMS | `sms_out/p2p/prices.csv.gz` | `sms_out/p2p/lookup/prices` | `SmsOutPrice` |
| `client.SmsOutOtpPrices()` | SMS | `sms_out/otp/prices.csv.gz` | `sms_out/otp/lookup/prices` | `SmsOutPrice` |
| `client.SmsInPrices()` | SMS | `sms_in/prices.csv.gz` | `sms_in/lookup/prices` | `SmsInPrice` |

Each accessor returns a `PricesService<TPrice>` (namespace `Didww.Api3.Prices`); the row classes are in `Didww.Api3.Resource.Prices`. The account needs a price plan of that service: without one, every call throws `DidwwPricesErrorKind.PricePlanNotAssigned`.

### Price lookups

```csharp
using Didww.Api3.Prices;
using Didww.Api3.Resource.Prices;

var result = await client.VoiceInPrices().LookupAsync(new Dictionary<string, string?>
{
    ["number"] = "18005550123",
    ["src_country_iso"] = "US"
});
Console.WriteLine(result.TotalRecords);
foreach (var price in result.Rows)
{
    if (price.Rate != null)
        Console.WriteLine($"{price.Destination} {price.SourceType}: {price.Rate} USD/min");
}

var voiceOut = await client.VoiceOutPrices().LookupAsync(new Dictionary<string, string?> { ["number"] = "493012345678" });
Console.WriteLine(voiceOut.Rows[0].Category); // VoiceOutPriceCategory
var a2p = await client.SmsOutA2PPrices().LookupAsync(new Dictionary<string, string?> { ["dst_number"] = "4915123456789" });
var smsIn = await client.SmsInPrices().LookupAsync(new Dictionary<string, string?> { ["dst_country_iso"] = "US" });
```

| Service | Filters |
|---------|---------|
| Voice IN | `number` (required), `src_country_iso` |
| Voice OUT | `number` (required); no other filter |
| SMS OUT and SMS IN | `dst_number`, `dst_country_iso`; at least one |

A missing or unknown filter throws an `ArgumentException` before any request; `null` and blank values are dropped. The server checks the value formats and answers a bad value with 400 (`DidwwPricesErrorKind.BadRequest`).

`LookupAsync` returns a `PriceLookup<TPrice>` with `Rows` and `TotalRecords`. It always holds at least one row: no match throws `DidwwPricesErrorKind.PriceNotFound`, never an empty result. In the rows, prices and fees are `decimal?` in USD (`null` when the row rejects the calls or messages, see `RejectCalls` and `RejectSms`; `SmsInPrice.Price` is a `decimal`), intervals and increments are `int` (`int?` for the scheduled `NextInitialIncrement` and `NextRecurringIncrement`), `SmsOutPrice.Mcc` and `Mnc` are `int?`, `VoiceOutPrice.EffectiveAt` is a `DateTimeOffset?` and `VoiceOutPrice.Category` is one of the `VoiceOutPriceCategory` values (`International`, `OriginBased`, `Local`).

The `Id` of an SMS OUT or SMS IN lookup row is derived from its content: it changes when the row or its price changes, so do not use it to track a destination over time. A Voice OUT `Id` stays the same when a re-import changes only the price. A Voice IN `Id` names the answering row of the plan.

### Price files

The files are gzipped CSV with a header row. The SDK writes them exactly as served and never decompresses them.

```csharp
var prices = client.VoiceInPrices();
const string path = "/var/data/voice_in_prices.csv.gz";

// Download unless the copy at the path is current
var file = await prices.FetchAsync(path);
Console.WriteLine(file.NotModified
    ? "answered 304, the file was kept"
    : $"downloaded {file.BytesWritten} bytes, last modified {file.LastModified:R}");

// Read the rows, keyed by the header line
await using (var stream = File.OpenRead(path))
{
    await foreach (var row in PriceFileReader.ReadRowsAsync(stream))
        Console.WriteLine($"{row["Destination"]}: {row["Rate (USD, excl. VAT)"]}");
}

// Lower-level calls
var status = await prices.HeadAsync(file.LastModified); // sent as If-Modified-Since; null sends none
// status.NotModified, status.ContentLength (null when unknown),
// status.FileName (from Content-Disposition, e.g. "voice_in_prices.csv.gz")

file = await prices.DownloadToFileAsync(path);
file = await prices.DownloadAsync(destinationStream); // any writable Stream
```

- `FetchAsync(path)` sends the modification time of the file at `path` as `If-Modified-Since`, when the file is there; an unchanged price file is answered 304 and the file is kept. A file written by `DownloadToFileAsync` or `FetchAsync` gets the `Last-Modified` of the response as its modification time, so the date matches the copy on disk. `FetchAsync` trusts the modification time of any file at `path`: a file copied or changed by other means can be answered 304 and kept, so replace such a file with `DownloadToFileAsync`.
- `DownloadToFileAsync` and `FetchAsync` stream the body into a temp file next to `path`, unique to the call, which gets the mode of a new file (`0666` minus the umask on Unix) and replaces `path` atomically only on a 200. A 304 or an error leaves an existing file untouched, the temp file is always removed, and concurrent downloads to one path do not mix their bytes. On Windows the replacement fails while `path` is open without `FileShare.Delete`.
- `DownloadAsync(destination, ifModifiedSince)` writes the body into `destination` only on a 200, as it arrives, so after a transfer error `destination` may hold part of it. An exception of `destination` propagates as is.
- `HeadAsync` and the downloads return a `PriceFile`: `StatusCode`, `LastModified` (`null` when absent), `ContentLength`, `FileName`, `BytesWritten` and `NotModified`.
- `PriceFileReader.ReadRows(stream)` and `ReadRowsAsync(stream)` gunzip a downloaded file and return each row as a dictionary keyed by the header line, as served (for example `Rate (USD, excl. VAT)`). A UTF-8 byte order mark is skipped. The input is a single gzip member, as the prices services serve it. Input that is not gzip, gzip data cut short (an empty input included), or a row whose field count differs from the header, throws an `InvalidDataException`. The rows are read lazily, so rows before the error have already been returned; the stream is left open.
- Only `Last-Modified` and `If-Modified-Since` are used. The service sends no `ETag`, and the SDK never sends `If-None-Match`, which would make the service ignore `If-Modified-Since`.
- A 503 on a price file is retried up to 2 times when its `Retry-After` is 60 seconds or less, after waiting that long (at least 1 s) plus a small random delay so that clients do not retry in step. Otherwise it throws `DidwwPricesErrorKind.ServiceUnavailable` with `RetryAfter`. Lookups are not retried.
- A 404 `price_not_found` throws `PriceNotFound`; any other 404, `price_plan_not_assigned` included, and any 404 on `HeadAsync`, which has no body, throws `PricePlanNotAssigned`. `DidwwPricesException.Code` holds the code of the answer, or `price_plan_not_assigned` for a 404 without one.
- Redirects are not followed. A status without its own kind in [Error Handling](#error-handling), such as a 3xx, a 202 or a 204, throws `DidwwPricesErrorKind.UnexpectedStatus`. An error is never turned into an empty file or an empty result.

## Filtering, Sorting, and Pagination

> See [`FILTERS.md`](FILTERS.md) for the list of `filter[KEY]` keys accepted by every list endpoint at API version `2026-10-22`.

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

Every exception of the SDK derives from `DidwwException` (namespace `Didww.Api3.Exception`). Messages never contain the API key or a token.

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

| Exception | Thrown for |
|-----------|------------|
| `DidwwApiException` | An error status of the JSON:API repositories and of `DownloadExportAsync`, with `HttpStatus` and `Errors`. |
| `DidwwClientException` | A failure on the client side: an error status of `UploadEncryptedFileAsync`, an upload or price lookup answer that cannot be parsed, a refused export URL, a broken export download, `UpdateAsync` on a resource without an `Id`. |
| `DidwwAuthException` | The token service, from any call that needs a new token. |
| `DidwwPricesException` | The prices services. |

`DidwwApiException` also reports the authentication failures of the API: a 401 still answered after the token was replaced once (`HttpStatus` 401), and a 403 such as `insufficient_scope` for a write with a `Read` token, which is never retried. `UploadEncryptedFileAsync` reports any error status, including a 401 after the retry and a 403, as a `DidwwClientException` whose `Message` holds the HTTP status. `DownloadExportAsync` does not follow redirects: any status other than 200, such as a 3xx or a 204, throws a `DidwwApiException`, and a redirect followed by a custom handler throws a `DidwwClientException`.

The token service and prices errors carry a `Kind`, which `catch ... when` tells apart:

```csharp
try
{
    await client.SmsOutA2PPrices().LookupAsync(new Dictionary<string, string?> { ["dst_number"] = "4915123456789" });
}
catch (DidwwAuthException e) when (e.Kind == DidwwAuthErrorKind.RateLimited)
{
    // Wait RetryAfter and try again; repeat while the kind is RateLimited.
    Console.WriteLine($"Token service rate limit, retry after {e.RetryAfter}");
}
catch (DidwwAuthException e) when (e.Kind == DidwwAuthErrorKind.InvalidClient)
{
    Console.WriteLine($"API key refused: {e.Message}");
}
catch (DidwwPricesException e) when (e.Kind == DidwwPricesErrorKind.PriceNotFound)
{
    Console.WriteLine("No price for this number");
}
catch (DidwwPricesException e) when (e.Kind == DidwwPricesErrorKind.TooManyRequests)
{
    Console.WriteLine($"Prices rate limit, retry after {e.RetryAfter}");
}
catch (DidwwException e)
{
    Console.WriteLine($"Error: {e.Message}");
}
```

**Token service** (`DidwwAuthException`, with `Kind`, `StatusCode`, `Error`, `ErrorDescription` and `RetryAfter`):

| `DidwwAuthErrorKind` | Thrown when |
|----------------------|-------------|
| `InvalidClient` | 401: the token service refused the API key. Not retried. |
| `InvalidRequest` | 400, e.g. `invalid_request` or `invalid_scope`. |
| `RateLimited` | 429. `RetryAfter` is the server's wait. Further mints fail without a request until `RetryAfter` plus a small random delay has passed; such a refusal reports the time still left in `RetryAfter`, so retry in a loop rather than once. |
| `ServerError` | 5xx on the last of the 4 attempts. |
| `ConnectionError` | Connection failure or timeout on the last attempt, or a TLS failure (not retried). |
| `InvalidGrant` | The token is empty or does not cover the requested audience. |
| `UnexpectedStatus` | Any other status, or a redirect followed by a custom handler. |

**Prices** (`DidwwPricesException`, with `Kind`, `StatusCode`, `Code`, `Errors` and `RetryAfter`):

| `DidwwPricesErrorKind` | Status |
|------------------------|--------|
| `BadRequest` | 400, e.g. a filter value of the wrong format |
| `NotAuthorized` | 401 after the token was replaced once |
| `AccessDenied` | 403 |
| `PricePlanNotAssigned` | 404 `price_plan_not_assigned`, and any other 404 except `price_not_found`, including any 404 on `HeadAsync`, which has no body |
| `PriceNotFound` | 404 `price_not_found`: no price matches the lookup |
| `NotAcceptable` | 406 |
| `TooManyRequests` | 429, with `RetryAfter`. Not retried. |
| `ServiceUnavailable` | 503, with `RetryAfter`. Retried on price files only, see [Price files](#price-files). |
| `UnexpectedStatus` | Any other status, or a redirect followed by a custom handler |
| `ConnectionError` | Connection failure, timeout, TLS failure or a response body that breaks off. Not retried. An exception writing the file or `Stream` propagates as is. |

The prices services can also throw a `DidwwAuthException`, `LookupAsync` an `ArgumentException` for invalid filters and a `DidwwClientException` when a 200 answer is not a JSON:API document it can parse or holds no prices. `DownloadToFileAsync` and `FetchAsync` also let the file system exceptions of the path (temp file, times, move) propagate as is. When the `CancellationToken` is cancelled, also while a 503 `Retry-After` is being waited out, the `OperationCanceledException` propagates as is.

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

For the SMS trunk configuration types, see [SMS Trunks](#sms-trunks).

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
| SmsTrunk | `SmsTrunk` | List, Find, Create, Update, Delete |
| SmsTrunkGroup | `SmsTrunkGroup` | List, Find, Create, Update, Delete |
| SmsTrunkRegenerateCredential | `SmsTrunkRegenerateCredential` | Create |
| SenderIdVerification | `SenderIdVerification` | List, Find, Update |
| DidReservation | `DidReservation` | List, Find, Create, Delete |
| CapacityPool | `CapacityPool` | List, Find |
| SharedCapacityGroup | `SharedCapacityGroup` | List, Find, Create, Update, Delete |
| Order | `Order` | List, Find, Create |
| Export | `Export` | List, Find, Create, Update |
| Address | `Address` | List, Find, Create, Delete |
| AddressVerification | `AddressVerification` | List, Create, Update |
| Identity | `Identity` | List, Find, Create, Update, Delete |
| EncryptedFile | `EncryptedFile` | List, Find, Delete |
| PermanentSupportingDocument | `PermanentSupportingDocument` | Create, Delete |
| Proof | `Proof` | Create, Delete |
| AddressRequirementValidation | `AddressRequirementValidation` | Create |
| DidHistory | `DidHistory` | List |
| EmergencyRequirement | `EmergencyRequirement` | List, Find |
| EmergencyRequirementValidation | `EmergencyRequirementValidation` | Create |
| EmergencyCallingService | `EmergencyCallingService` | List, Find, Delete |
| EmergencyVerification | `EmergencyVerification` | List, Find, Create, Update |
| VoiceInPrice | `client.VoiceInPrices()` | Lookup, Head, Download, DownloadToFile, Fetch |
| VoiceOutPrice | `client.VoiceOutPrices()` | Lookup, Head, Download, DownloadToFile, Fetch |
| SmsOutPrice | `client.SmsOutA2PPrices()`, `client.SmsOutP2PPrices()`, `client.SmsOutOtpPrices()` | Lookup, Head, Download, DownloadToFile, Fetch |
| SmsInPrice | `client.SmsInPrices()` | Lookup, Head, Download, DownloadToFile, Fetch |

> **Note:** The price rows are served by the prices services outside the JSON:API, see [Prices](#prices).

## Date and Datetime Fields

The SDK distinguishes between date-only and datetime fields:

- **Datetime fields** are deserialized as `DateTimeOffset?`:
  - `CreatedAt` — present on most resources
  - `UpdatedAt` — `SmsTrunk`, `SmsTrunkGroup`
  - `ExpiresAt` — `Did`, `DidReservation`, `Proof`, `EncryptedFile` (nullable)
  - `ActivatedAt` — `EmergencyCallingService`, `SenderIdVerification` (nullable)
  - `CanceledAt` — `EmergencyCallingService` (nullable)
  - `ClosedAt` — `SenderIdVerification` (nullable)
  - `EffectiveAt` — `VoiceOutPrice` price lookup rows (nullable)
- **Date-only fields** (`Identity.BirthDate`) are deserialized as `DateOnly?`.
- **Date-only fields kept as strings** remain as `string?`:
  - `CapacityPool.RenewDate`, `EmergencyCallingService.RenewDate` — `"YYYY-MM-DD"` (nullable)
  - `SenderIdVerification.RenewDate` — `"YYYY-MM-DD"`, `null` once the verification is closed
  - `BilledFrom`, `BilledTo` — `DidOrderItem` (and the available and reservation DID order items), `EmergencyOrderItem`
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
`DidHistoryAction`, `DidHistoryMethod`, `NumberType`, `Pop`, `SenderIdVerificationStatus`, `SenderType`,
`SmsHttpMethod`, `SmsBodyType`, `VoiceOutPriceCategory`

\* `ReplaceCli` and `RandomizeCli` require additional account configuration. Contact DIDWW support to enable these values.

`ExportType` includes `SmsIn` and `SmsOut`. The values of the `trunk_type` filter of `sms_in` exports are the constants of `ExportTrunkTypes` (namespace `Didww.Api3.Resource`), and the SMPP server locations those of `SmppServerLocations` (namespace `Didww.Api3.Resource.Configuration.SmsTrunkConfiguration`).

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
2. Commit and push to `main` (4.x), or to `release-3` for a 3.x patch release
3. Create and push a version tag matching that version:

```bash
git tag v4.0.0
git push origin v4.0.0
```

The [release workflow](.github/workflows/release.yml) will automatically build, test, and publish the package to [NuGet](https://www.nuget.org/). For tags on `main` (4.x), it fails when the tag does not match the version in the `.csproj`.

## Contributing

Bug reports and pull requests are welcome on GitHub at https://github.com/didww/didww-api-3-dotnet-sdk

## License

The package is available as open source under the terms of the [MIT License](https://opensource.org/licenses/MIT).
