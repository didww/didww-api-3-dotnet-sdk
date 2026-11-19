using System.Collections.Concurrent;
using System.Net;
using Didww.Api3.Auth;
using Didww.Api3.Callback;
using Didww.Api3.Exception;
using Didww.Api3.Http;
using Didww.Api3.Prices;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Configuration.AuthenticationMethod;
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using Didww.Api3.Resource.Enums;
using Didww.Api3.Resource.OrderItem;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Examples;

// Runs the code snippets of the SDK README (README.md at the repository root)
// against the sandbox, one check per snippet, and prints
// "Results: N passed, M failed". The process exits with 1 when a check fails.
//
// Placeholders such as "uuid" or "trunk-uuid" are replaced with resources of the
// sandbox account or with resources the check creates. Everything a check
// creates is deleted again, except orders, exports and verifications, which
// cannot be deleted; a DID or Sender ID verification a check changes is
// restored. The address and emergency verifications it creates are printed on
// lines starting with "REVIEW", so that they can be approved or rejected.
// Only the snippets that would talk to production are skipped.
//
// The proxy and Redis snippets need a server:
//   DIDWW_README_PROXY_URL  an HTTP proxy, e.g. http://localhost:3128
//   DIDWW_README_REDIS_URL  a Redis server, e.g. redis://localhost:6379/0
// Without the variable, that check is reported as skipped. The shell snippets
// (installation, development, release) are not checked.
//
// Usage: ... -- test-readme
public static class TestReadmeExample
{
    private static DidwwClient _client = null!;
    private static string _apiKey = "";
    private static string _suffix = "";
    private static int _passed;
    private static int _failed;
    private static readonly List<string> Skipped = new();
    private static readonly List<string> Failures = new();

    public static async Task<int> RunAsync(DidwwClient client)
    {
        if (client.Credentials.Environment != DidwwEnvironment.Sandbox)
            throw new InvalidOperationException("test-readme runs against the sandbox only");
        _client = client;
        _apiKey = client.Credentials.ApiKey;
        _suffix = Guid.NewGuid().ToString("N")[..8];
        _passed = 0;
        _failed = 0;
        Skipped.Clear();
        Failures.Clear();

        await UsageAndConfigurationAsync();
        await ReadOnlyResourcesAsync();
        await DidsAsync();
        await VoiceTrunksAsync();
        await SmsAsync();
        await OrdersAndCapacityAsync();
        await IdentitiesAndExportsAsync();
        await VerificationsAndHistoryAsync();
        await PricesAsync();
        await QueryAndDirtyPatchAsync();
        await ErrorHandlingAsync();
        await EncryptionWebhookAndDatesAsync();

        PrintSummary();
        return _failed > 0 ? 1 : 0;
    }

    // ─── Usage, Configuration, Authentication ──────────────────

    private static async Task UsageAndConfigurationAsync()
    {
        Section("Usage");
        await Check("Usage: balance and DID groups with SKUs", async () =>
        {
            // README: DidwwEnvironment.Production; the checks run in the sandbox
            var client = DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials(_apiKey, DidwwEnvironment.Sandbox))
                .Build();
            var balance = (await client.Balance().FindAsync()).Data;
            Console.WriteLine($"    Balance: {balance.BalanceAmount}, Credit: {balance.Credit}");
            var queryParams = new QueryParams().Include("stock_keeping_units");
            var didGroups = await client.DidGroups().ListAsync(queryParams);
            Expect(didGroups.Data.Count > 0, "no DID groups");
            Console.WriteLine($"    {didGroups.Data.Count} DID groups, the first with {didGroups.Data[0].StockKeepingUnits?.Count ?? 0} SKU(s)");
        });

        Section("Configuration");
        Skip("Configuration: production client", "production only; the Usage check runs the same calls in the sandbox");
        await Check("Configuration: sandbox client with Read scope and a logger", async () =>
        {
            // README: loggerFactory.CreateLogger("Didww"); any ILogger works. A new
            // token store makes the client mint a token, which is logged.
            var logger = new RecordingLogger();
            var client = DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials(_apiKey, DidwwEnvironment.Sandbox))
                .SetScope(DidwwScope.Read)
                .SetLogger(logger)
                .SetTokenStore(new MemoryTokenStore())
                .Build();
            var balance = (await client.Balance().FindAsync()).Data;
            Console.WriteLine($"    Balance: {balance.BalanceAmount}; {logger.Lines.Count} log line(s)");
            Expect(logger.Lines.Any(line => line.Contains("DIDWW token minted")), "no 'DIDWW token minted' event was logged");
            Expect(logger.Lines.All(line => !line.Contains(_apiKey)), "the API key was logged");
        });

        Section("Custom HTTP Handler");
        await Check("Custom HTTP handler with a proxy", async () =>
        {
            // README: new WebProxy("http://proxy.example.com:8080") and DidwwEnvironment.Production.
            // The check sends sandbox requests through the proxy of DIDWW_README_PROXY_URL;
            // RecordingProxy is that WebProxy, noting the hosts it is asked to proxy.
            var proxy = new RecordingProxy(RequiredSetting("DIDWW_README_PROXY_URL"));
            using var handler = new HttpClientHandler
            {
                Proxy = proxy,
                UseProxy = true,
                AllowAutoRedirect = false
            };
            var client = DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials(_apiKey, DidwwEnvironment.Sandbox))
                .SetInnerHandler(handler)
                .SetTokenStore(new MemoryTokenStore()) // a new store, so that the token is minted through the proxy too
                .Build();
            var balance = (await client.Balance().FindAsync()).Data;
            var publicKeys = await client.PublicKeys().ListAsync();
            Console.WriteLine($"    balance {balance.BalanceAmount}, {publicKeys.Data.Count} public keys");
            Console.WriteLine($"    proxied hosts: {string.Join(", ", proxy.Hosts.Keys.Order())}");
            foreach (var url in new[] { DidwwEnvironment.Sandbox.GetAuthUrl(), DidwwEnvironment.Sandbox.GetBaseUrl() })
                Expect(proxy.Hosts.ContainsKey(new Uri(url).Host), $"no request to {new Uri(url).Host} went through the proxy");
        });

        Section("Scope");
        await Check("Scope: a Read token reads; a write is answered 403", async () =>
        {
            var readOnly = DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials(_apiKey, DidwwEnvironment.Sandbox))
                .SetScope(DidwwScope.Read)
                .Build();
            var balance = (await readOnly.Balance().FindAsync()).Data;
            Console.WriteLine($"    read: balance {balance.BalanceAmount}");
            try
            {
                var created = (await readOnly.VoiceInTrunkGroups().CreateAsync(
                    new VoiceInTrunkGroup { Name = $"README scope check {_suffix}" })).Data;
                await _client.VoiceInTrunkGroups().DeleteAsync(created.Id!);
                throw new InvalidOperationException("the write with a Read token was accepted");
            }
            catch (DidwwApiException e) when (e.HttpStatus == 403)
            {
                Console.WriteLine($"    write: 403 {e.Errors.FirstOrDefault()?.Code}");
            }
        });

        Section("Token cache");
        await Check("Token cache: DistributedCacheTokenStore over RedisCache", async () =>
        {
            var configuration = RedisConfiguration(RequiredSetting("DIDWW_README_REDIS_URL"));
            // README: new RedisCache(new RedisCacheOptions { Configuration = "localhost:6379" }).
            // An instance name of this run's own makes the first client mint a token.
            var options = new RedisCacheOptions { Configuration = configuration, InstanceName = $"didww-readme-{_suffix}:" };

            using var cache = new RedisCache(options);
            var store = new CountingTokenStore(new DistributedCacheTokenStore(cache));
            var client = DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials(_apiKey, DidwwEnvironment.Sandbox))
                .SetTokenStore(store)
                .Build();
            await client.Balance().FindAsync();
            Expect(store.Writes.Count == 1, $"the first client stored {store.Writes.Count} tokens, expected 1");
            var key = store.Writes[0];
            Expect(key.StartsWith("didww:token:v1:", StringComparison.Ordinal), "the key does not start with didww:token:v1:");

            // A second cache and client, with a connection of their own, read the token from Redis
            using var otherCache = new RedisCache(options);
            try
            {
                Expect(await otherCache.GetAsync(key) != null, "the token is not in Redis");
                var otherStore = new CountingTokenStore(new DistributedCacheTokenStore(otherCache));
                var otherClient = DidwwClient.NewBuilder()
                    .SetCredentials(new DidwwCredentials(_apiKey, DidwwEnvironment.Sandbox))
                    .SetTokenStore(otherStore)
                    .Build();
                await otherClient.Balance().FindAsync();
                Expect(otherStore.Hits > 0 && otherStore.Writes.Count == 0,
                    "the second client minted a token instead of reusing the one in Redis");
                Console.WriteLine($"    minted and stored by the first client, reused by the second ({otherStore.Hits} read(s))");
            }
            finally
            {
                await otherCache.RemoveAsync(key);
            }
        });
        await Check("Token cache: own ITokenStore", async () =>
        {
            var store = new DictionaryTokenStore();
            var client = DidwwClient.NewBuilder()
                .SetCredentials(new DidwwCredentials(_apiKey, DidwwEnvironment.Sandbox))
                .SetTokenStore(store)
                .Build();
            await client.Balance().FindAsync();
            Expect(store.Tokens.Count == 1, $"{store.Tokens.Count} tokens stored, expected 1");
            var token = store.Tokens.Values.Single();
            Expect(token.ExpiresIn > 0, "the token has no expires_in");
            Expect(!token.ToString().Contains(token.AccessToken), "DidwwToken.ToString shows the access token");
            Console.WriteLine($"    stored a token for {string.Join(", ", token.Audience)}, expires in {token.ExpiresIn} s");
        });
    }

    // ─── Read-Only Resources ──────────────────────────────────

    private static async Task ReadOnlyResourcesAsync()
    {
        Section("Read-Only Resources");
        await Check("Read-only resources", async () =>
        {
            var countries = await _client.Countries().ListAsync();
            var country = (await _client.Countries().FindAsync(countries.Data[0].Id!)).Data;
            Console.WriteLine($"    countries: {countries.Data.Count}, found {country.Name}");
            var regions = await _client.Regions().ListAsync();
            Console.WriteLine($"    regions: {regions.Data.Count}");
            var cities = await _client.Cities().ListAsync();
            Console.WriteLine($"    cities: {cities.Data.Count}");
            var areas = await _client.Areas().ListAsync();
            Console.WriteLine($"    areas: {areas.Data.Count}");
            var prefixes = await _client.NanpaPrefixes().ListAsync();
            Console.WriteLine($"    NANPA prefixes: {prefixes.Data.Count}");
            var groups = await _client.DidGroups().ListAsync(new QueryParams()
                .Include("stock_keeping_units")
                .Filter("number_type", "toll_free"));
            Expect(groups.Data.All(g => g.NumberType == NumberType.TollFree), "a DID group of another number type");
            Console.WriteLine($"    toll-free DID groups: {groups.Data.Count}");
            var available = await _client.AvailableDids().ListAsync();
            Console.WriteLine($"    available DIDs: {available.Data.Count}");
            var proofTypes = await _client.ProofTypes().ListAsync();
            Console.WriteLine($"    proof types: {proofTypes.Data.Count}");
            var publicKeys = await _client.PublicKeys().ListAsync();
            Expect(publicKeys.Data.Count >= 2, "fewer than 2 public keys");
            Console.WriteLine($"    public keys: {publicKeys.Data.Count}");
            var requirements = await _client.AddressRequirements().ListAsync();
            Console.WriteLine($"    address requirements: {requirements.Data.Count}");
            var balance = (await _client.Balance().FindAsync()).Data;
            Console.WriteLine($"    balance: {balance.TotalBalance}");
        });
    }

    // ─── DIDs ─────────────────────────────────────────────────

    private static async Task DidsAsync()
    {
        Section("DIDs");
        await Check("DIDs: list, then update a DID's description, capacity and trunk (restored)", async cleanup =>
        {
            var dids = await _client.Dids().ListAsync();
            Console.WriteLine($"    {dids.Data.Count} DIDs");
            var original = await FirstDidAsync();
            var trunk = await CreateTrunkAsync("README DID trunk");
            cleanup.Add("trunk", () => _client.VoiceInTrunks().DeleteAsync(trunk.Id!));
            cleanup.Add("DID", () => RestoreDidAsync(original));

            var did = (await _client.Dids().FindAsync(original.Id!)).Data;
            did.Description = "Updated";
            did.CapacityLimit = 20;
            did.VoiceInTrunk = VoiceInTrunk.Build(trunk.Id!);
            did = (await _client.Dids().UpdateAsync(did)).Data;
            Expect(did.Description == "Updated" && did.CapacityLimit == 20, "the update was not applied");
            Console.WriteLine($"    updated {did.Number}");
        });

        await Check("DIDs: SMS routing to a trunk, to a group, unrouted (restored)", async cleanup =>
        {
            var did = await FirstSmsDidAsync();
            var trunk = await CreateSmsTrunkAsync("README SMS trunk");
            cleanup.Add("SMS trunk", () => _client.SmsTrunks().DeleteAsync(trunk.Id!));
            var member = await CreateSmsTrunkAsync("README SMS group trunk");
            cleanup.Add("SMS group trunk", () => DeleteIgnoringNotFoundAsync(() => _client.SmsTrunks().DeleteAsync(member.Id!)));
            var group = (await _client.SmsTrunkGroups().CreateAsync(new SmsTrunkGroup
            {
                Name = $"README SMS group {_suffix}",
                SmsTrunks = new List<SmsTrunk> { SmsTrunk.Build(member.Id!) }
            })).Data;
            cleanup.Add("SMS trunk group and its trunk", () => _client.SmsTrunkGroups().DeleteAsync(group.Id!));
            cleanup.Add("DID SMS routing", () => RestoreSmsRoutingAsync(did));

            // sends sms_trunk and sms_trunk_group: null
            var route = Did.Build(did.Id!);
            route.SmsTrunk = SmsTrunk.Build(trunk.Id!);
            await _client.Dids().UpdateAsync(route);
            await ExpectSmsRoutingAsync(did.Id!, trunk.Id, null);

            var routed = await _client.Dids().ListAsync(new QueryParams()
                .Filter("sms_trunk.id", trunk.Id!)
                .Include("sms_trunk", "sms_trunk_group"));
            Expect(routed.Data.Any(d => d.Id == did.Id), "the DID is not listed under its SMS trunk");

            // sends sms_trunk_group and sms_trunk: null
            var toGroup = Did.Build(did.Id!);
            toGroup.SmsTrunkGroup = SmsTrunkGroup.Build(group.Id!);
            await _client.Dids().UpdateAsync(toGroup);
            await ExpectSmsRoutingAsync(did.Id!, null, group.Id);

            // unroute: sends sms_trunk: null and sms_trunk_group: null
            var unroute = Did.Build(did.Id!);
            unroute.SmsTrunk = null;
            unroute.SmsTrunkGroup = null;
            await _client.Dids().UpdateAsync(unroute);
            await ExpectSmsRoutingAsync(did.Id!, null, null);
        });
    }

    // ─── Voice In Trunks, Groups, Voice Out Trunks ────────────

    private static async Task VoiceTrunksAsync()
    {
        Section("Voice In Trunks");
        await Check("Voice In Trunks: create SIP and PSTN, update, delete", async cleanup =>
        {
            var sipConfig = new SipConfiguration
            {
                Username = "myuser",
                Host = "203.0.113.1",
                Port = 5060,
                Codecs = new List<Codec> { Codec.Pcma, Codec.Pcmu, Codec.TelephoneEvent },
                IceMode = IceMode.Accept,
                TransportProtocolId = TransportProtocol.UDP,
                SstRefreshMethodId = SstRefreshMethod.INVITE,
                MediaEncryptionMode = MediaEncryptionMode.Disabled
            };
            var trunk = new VoiceInTrunk { Name = $"My SIP Trunk {_suffix}", Configuration = sipConfig };
            var trunkResponse = await _client.VoiceInTrunks().CreateAsync(trunk);
            var sipId = trunkResponse.Data.Id!;
            cleanup.Add("SIP trunk", () => DeleteIgnoringNotFoundAsync(() => _client.VoiceInTrunks().DeleteAsync(sipId)));
            Expect(trunkResponse.Data.Configuration is SipConfiguration, "the trunk is not a SIP trunk");

            var pstnTrunk = new VoiceInTrunk
            {
                Name = $"My PSTN Trunk {_suffix}",
                Configuration = new PstnConfiguration { Dst = "558540420024" }
            };
            var pstn = (await _client.VoiceInTrunks().CreateAsync(pstnTrunk)).Data;
            cleanup.Add("PSTN trunk", () => _client.VoiceInTrunks().DeleteAsync(pstn.Id!));
            Expect(pstn.Configuration is PstnConfiguration { Dst: "558540420024" }, "the PSTN trunk has another dst");

            var existingTrunk = VoiceInTrunk.Build(sipId);
            existingTrunk.Name = $"Renamed trunk {_suffix}";
            var renamed = (await _client.VoiceInTrunks().UpdateAsync(existingTrunk)).Data;
            Expect(renamed.Name == existingTrunk.Name, "the trunk was not renamed");

            await _client.VoiceInTrunks().DeleteAsync(sipId);
            Console.WriteLine($"    SIP trunk {sipId} and PSTN trunk {pstn.Id} created, SIP trunk renamed and deleted");
        });
        await Check("Voice In Trunks: point of presence", async cleanup =>
        {
            var created = await CreateTrunkAsync("README pop trunk");
            cleanup.Add("trunk", () => _client.VoiceInTrunks().DeleteAsync(created.Id!));
            var popUpdate = VoiceInTrunk.Build(created.Id!);
            popUpdate.Pop = Pop.Ams;
            var updated = (await _client.VoiceInTrunks().UpdateAsync(popUpdate)).Data;
            Expect(updated.Pop == Pop.Ams, $"pop is {updated.Pop}");
        });

        Section("SIP Registration");
        await Check("SIP registration: enable, then disable by setting Host", async cleanup =>
        {
            var sipConfig = new SipConfiguration
            {
                EnabledSipRegistration = true,
                UseDidInRuri = true,
                CnamLookup = true
            };
            var trunk = new VoiceInTrunk { Name = $"Office (registered) {_suffix}", Configuration = sipConfig };
            var response = await _client.VoiceInTrunks().CreateAsync(trunk);
            cleanup.Add("trunk", () => _client.VoiceInTrunks().DeleteAsync(response.Data.Id!));
            var created = (SipConfiguration)response.Data.Configuration!;
            Expect(!string.IsNullOrEmpty(created.IncomingAuthUsername), "no IncomingAuthUsername was generated");
            Expect(!string.IsNullOrEmpty(created.IncomingAuthPassword), "no IncomingAuthPassword was generated");

            var disable = new SipConfiguration { Host = "sip.example.com" };
            var existing = VoiceInTrunk.Build(response.Data.Id!);
            existing.Configuration = disable;
            await _client.VoiceInTrunks().UpdateAsync(existing);
            var after = (SipConfiguration)(await _client.VoiceInTrunks().FindAsync(response.Data.Id!)).Data.Configuration!;
            Expect(after.EnabledSipRegistration == false, "SIP registration is still enabled");
            Expect(after.UseDidInRuri == false, "UseDidInRuri is still true");
        });

        Section("Voice In Trunk Groups");
        await Check("Voice In Trunk Groups: create", async cleanup =>
        {
            var group = new VoiceInTrunkGroup
            {
                Name = $"Primary Group {_suffix}",
                CapacityLimit = 50
            };
            var created = (await _client.VoiceInTrunkGroups().CreateAsync(group)).Data;
            cleanup.Add("trunk group", () => _client.VoiceInTrunkGroups().DeleteAsync(created.Id!));
            Expect(created.CapacityLimit == 50, $"capacity limit is {created.CapacityLimit}");
        });

        Section("Voice Out Trunks");
        await Check("Voice Out Trunks: create with credentials and IP authentication", async cleanup =>
        {
            // README: OnCliMismatchAction.ReplaceCli and a DefaultDid. ReplaceCli needs
            // account configuration (see Enums), so the check rejects the call instead
            // and sets no default DID.
            var trunk = new VoiceOutTrunk
            {
                Name = $"My Outbound Trunk {_suffix}",
                OnCliMismatchAction = OnCliMismatchAction.RejectCall,
                AuthenticationMethod = new CredentialsAndIpAuthenticationMethod
                {
                    AllowedSipIps = new List<string> { "203.0.113.0/24" },
                },
                Codecs = new List<VoiceOutCodec> { VoiceOutCodec.Opus, VoiceOutCodec.Pcma, VoiceOutCodec.TelephoneEvent }
            };
            var response = await _client.VoiceOutTrunks().CreateAsync(trunk);
            cleanup.Add("Voice OUT trunk", () => _client.VoiceOutTrunks().DeleteAsync(response.Data.Id!));
            var auth = response.Data.AuthenticationMethod as CredentialsAndIpAuthenticationMethod;
            Expect(!string.IsNullOrEmpty(auth?.Username), "no server-generated username");
            Expect(response.Data.Codecs?.SequenceEqual(trunk.Codecs!) == true,
                $"codecs are {string.Join(", ", response.Data.Codecs ?? new List<VoiceOutCodec>())}");
        });
    }

    // ─── SMS Trunks, SMS Trunk Groups, Sender ID Verifications ─

    private static async Task SmsAsync()
    {
        Section("SMS Trunks");
        await Check("SMS Trunks: create HTTP IN, update, clear with a generic configuration, list by type", async cleanup =>
        {
            var trunk = (await _client.SmsTrunks().CreateAsync(new SmsTrunk
            {
                Name = $"Inbound webhook {_suffix}",
                Configuration = new HttpInConfiguration
                {
                    Url = "https://example.com/sms",
                    HttpMethod = SmsHttpMethod.Post,
                    BodyType = SmsBodyType.Json,
                    Body = "{\"text\":\"{SMS_TEXT}\",\"from\":\"{SMS_SRC_ADDR}\",\"to\":\"{SMS_DST_ADDR}\"}"
                }
            })).Data;
            cleanup.Add("SMS trunk", () => _client.SmsTrunks().DeleteAsync(trunk.Id!));

            var update = SmsTrunk.Build(trunk.Id!);
            update.Configuration = new HttpInConfiguration { Url = "https://example.com/sms/v2" };
            var updated = (await _client.SmsTrunks().UpdateAsync(update)).Data;
            Expect(updated.Configuration is HttpInConfiguration { Url: "https://example.com/sms/v2", HttpMethod: SmsHttpMethod.Post },
                "the URL was not updated, or the method was not kept");

            var clear = SmsTrunk.Build(trunk.Id!);
            clear.Configuration = new GenericSmsTrunkConfiguration("http_in", new JObject
            {
                ["http_method"] = "get",
                ["body_type"] = JValue.CreateNull(),
                ["body"] = JValue.CreateNull()
            });
            var cleared = (await _client.SmsTrunks().UpdateAsync(clear)).Data;
            Expect(cleared.Configuration is HttpInConfiguration { HttpMethod: SmsHttpMethod.Get, BodyType: null, Body: null },
                "the body was not cleared");

            var trunks = await _client.SmsTrunks().ListAsync(new QueryParams()
                .Filter("configuration.type", "http_in,sms_to_email")
                .Include("sms_trunk_group"));
            Expect(trunks.Data.All(t => t.Configuration?.ConfigurationType is "http_in" or "sms_to_email"),
                "the list has a trunk of another configuration type");
            Console.WriteLine($"    {trunks.Data.Count} http_in and sms_to_email trunks");
        });
        await Check("SMS Trunks: regenerate the credentials of an HTTP OUT trunk", async cleanup =>
        {
            var created = await CreateHttpOutTrunkAsync("README HTTP OUT");
            cleanup.Add("SMS trunk", () => _client.SmsTrunks().DeleteAsync(created.Id!));
            var outTrunkId = created.Id!;
            var passwordBefore = (created.Configuration as HttpOutConfiguration)?.AuthPassword;

            await _client.SmsTrunkRegenerateCredentials().CreateAsync(
                new SmsTrunkRegenerateCredential { SmsTrunk = SmsTrunk.Build(outTrunkId) });

            // The response carries no credentials: read the trunk again
            var outTrunk = (await _client.SmsTrunks().FindAsync(outTrunkId)).Data;
            switch (outTrunk.Configuration)
            {
                case HttpOutConfiguration http:
                    Expect(!string.IsNullOrEmpty(http.AuthName) && !string.IsNullOrEmpty(http.AuthPassword), "no credentials");
                    if (passwordBefore != null)
                        Expect(http.AuthPassword != passwordBefore, "the password did not change");
                    break;
                default:
                    throw new InvalidOperationException($"unexpected configuration {outTrunk.Configuration?.ConfigurationType}");
            }
        });

        Section("SMS Trunk Groups");
        await Check("SMS Trunk Groups: create, add a trunk, empty the group", async cleanup =>
        {
            var first = await CreateSmsTrunkAsync("README group trunk 1");
            cleanup.Add("SMS trunk 1", () => DeleteIgnoringNotFoundAsync(() => _client.SmsTrunks().DeleteAsync(first.Id!)));
            var second = await CreateSmsTrunkAsync("README group trunk 2");
            cleanup.Add("SMS trunk 2", () => DeleteIgnoringNotFoundAsync(() => _client.SmsTrunks().DeleteAsync(second.Id!)));

            var group = (await _client.SmsTrunkGroups().CreateAsync(new SmsTrunkGroup
            {
                Name = $"Inbound SMS {_suffix}",
                SmsTrunks = new List<SmsTrunk> { SmsTrunk.Build(first.Id!) }
            })).Data;
            cleanup.Add("SMS trunk group", () => _client.SmsTrunkGroups().DeleteAsync(group.Id!));
            Expect(group.SmsTrunksCount == 1, $"SmsTrunksCount is {group.SmsTrunksCount}");

            var current = (await _client.SmsTrunkGroups().FindAsync(group.Id!, new QueryParams().Include("sms_trunks"))).Data;
            var update = SmsTrunkGroup.Build(group.Id!);
            update.SmsTrunks = (current.SmsTrunks ?? new List<SmsTrunk>())
                .Select(t => SmsTrunk.Build(t.Id!))
                .Append(SmsTrunk.Build(second.Id!))
                .ToList();
            await _client.SmsTrunkGroups().UpdateAsync(update);
            var extended = (await _client.SmsTrunkGroups().FindAsync(group.Id!)).Data;
            Expect(extended.SmsTrunksCount == 2, $"SmsTrunksCount is {extended.SmsTrunksCount} after adding a trunk");

            // An empty list removes every trunk from the group
            var empty = SmsTrunkGroup.Build(group.Id!);
            empty.SmsTrunks = new List<SmsTrunk>();
            await _client.SmsTrunkGroups().UpdateAsync(empty);
            var emptied = (await _client.SmsTrunkGroups().FindAsync(group.Id!)).Data;
            Expect(emptied.SmsTrunksCount == 0, $"SmsTrunksCount is {emptied.SmsTrunksCount} after emptying");
        });

        Section("Sender ID Verifications");
        await Check("Sender ID verifications: route and unroute an active one (restored)", async cleanup =>
        {
            var verifications = (await _client.SenderIdVerifications().ListAsync(new QueryParams()
                .Filter("status", "active")
                .Include("sms_trunk"))).Data;
            Console.WriteLine($"    {verifications.Count} active verification(s)");
            if (verifications.Count == 0)
                throw new SkipException("no active Sender ID verification on the sandbox account");
            var original = verifications[0];

            // http_out, smpp_esme or smpp_smsc trunks only
            var trunk = await CreateHttpOutTrunkAsync("README sender ID trunk");
            cleanup.Add("SMS trunk", () => _client.SmsTrunks().DeleteAsync(trunk.Id!));
            cleanup.Add("Sender ID verification trunk", async () =>
            {
                var restore = SenderIdVerification.Build(original.Id!);
                restore.SmsTrunk = original.SmsTrunk == null ? null : SmsTrunk.Build(original.SmsTrunk.Id!);
                await _client.SenderIdVerifications().UpdateAsync(restore);
            });

            var route = SenderIdVerification.Build(verifications[0].Id!);
            route.SmsTrunk = SmsTrunk.Build(trunk.Id!);
            await _client.SenderIdVerifications().UpdateAsync(route);

            // Outbound messages stop until a trunk is assigned again
            var unroute = SenderIdVerification.Build(verifications[0].Id!);
            unroute.SmsTrunk = null;
            await _client.SenderIdVerifications().UpdateAsync(unroute);
            var after = (await _client.SenderIdVerifications().FindAsync(original.Id!, new QueryParams().Include("sms_trunk"))).Data;
            Expect(after.SmsTrunk == null, "the verification still has an SMS trunk");
        });
    }

    // ─── Orders, DID Reservations, Shared Capacity Groups ─────

    private static async Task OrdersAndCapacityAsync()
    {
        Section("Orders");
        await Check("Orders: by SKU", async () =>
        {
            var groups = (await _client.DidGroups().ListAsync(new QueryParams()
                .Include("stock_keeping_units")
                .Page(1, 10))).Data;
            var sku = groups.Select(g => g.StockKeepingUnits?.FirstOrDefault()).FirstOrDefault(s => s != null)
                      ?? throw new SkipException("no DID group with SKUs");
            var order = new Order
            {
                AllowBackOrdering = true,
                Items = new List<OrderItemBase>
                {
                    new DidOrderItem { SkuId = sku.Id, Qty = 2 }
                }
            };
            var response = await _client.Orders().CreateAsync(order);
            Console.WriteLine($"    order {response.Data.Id}: {response.Data.Status}, amount {response.Data.Amount}");
        });
        await Check("Orders: an available DID", async () =>
        {
            var available = (await _client.AvailableDids().ListAsync(new QueryParams()
                .Include("did_group.stock_keeping_units"))).Data
                .FirstOrDefault(ad => ad.DidGroup?.StockKeepingUnits is { Count: > 0 })
                ?? throw new SkipException("no available DID with SKUs");
            var order = new Order
            {
                Items = new List<OrderItemBase>
                {
                    new AvailableDidOrderItem
                    {
                        SkuId = available.DidGroup!.StockKeepingUnits![0].Id,
                        AvailableDidId = available.Id
                    }
                }
            };
            var response = await _client.Orders().CreateAsync(order);
            Console.WriteLine($"    order {response.Data.Id} for {available.Number}: {response.Data.Status}");
        });
        await Check("Orders: capacity", async () =>
        {
            var pool = (await _client.CapacityPools().ListAsync(new QueryParams().Page(1, 1))).Data.FirstOrDefault()
                       ?? throw new SkipException("no capacity pool");
            var order = new Order
            {
                Items = new List<OrderItemBase>
                {
                    // README: Qty = 1; a pool can ask for more per order
                    new CapacityOrderItem { CapacityPoolId = pool.Id, Qty = Math.Max(1, pool.MinimumQtyPerOrder ?? 1) }
                }
            };
            var response = await _client.Orders().CreateAsync(order);
            Console.WriteLine($"    order {response.Data.Id} in {pool.Name}: {response.Data.Status}");
        });

        Section("DID Reservations");
        await Check("DID reservations: create and delete", async () =>
        {
            var available = (await _client.AvailableDids().ListAsync(new QueryParams().Page(1, 1))).Data.FirstOrDefault()
                            ?? throw new SkipException("no available DID");
            var reservation = new DidReservation();
            reservation.Description = "Reserved for client";
            reservation.AvailableDid = AvailableDid.Build(available.Id!);
            var created = (await _client.DidReservations().CreateAsync(reservation)).Data;
            await _client.DidReservations().DeleteAsync(created.Id!);
            Console.WriteLine($"    reserved {available.Number} and deleted the reservation");
        });

        Section("Shared Capacity Groups");
        await Check("Shared capacity groups: create", async cleanup =>
        {
            var pool = (await _client.CapacityPools().ListAsync(new QueryParams().Page(1, 1))).Data.FirstOrDefault()
                       ?? throw new SkipException("no capacity pool");
            var group = new SharedCapacityGroup();
            group.Name = $"Shared Group {_suffix}";
            group.SharedChannelsCount = pool.MinimumLimit ?? 1; // README: 20; the pool decides how many are free
            group.CapacityPool = CapacityPool.Build(pool.Id!);
            var created = (await _client.SharedCapacityGroups().CreateAsync(group)).Data;
            cleanup.Add("shared capacity group", () => _client.SharedCapacityGroups().DeleteAsync(created.Id!));
            Console.WriteLine($"    {created.Name}: {created.SharedChannelsCount} shared channels");
        });
    }

    // ─── Identities, Addresses, Exports ───────────────────────

    private static async Task IdentitiesAndExportsAsync()
    {
        Section("Identities");
        await Check("Identities: create", async cleanup =>
        {
            var countryId = await FirstCountryIdAsync();
            var identity = await CreateIdentityAsync(countryId);
            cleanup.Add("identity", () => _client.Identities().DeleteAsync(identity.Id!));
            Console.WriteLine($"    identity {identity.Id}: {identity.FirstName} {identity.LastName}");
        });

        Section("Addresses");
        await Check("Addresses: create", async cleanup =>
        {
            var countryId = await FirstCountryIdAsync();
            var identity = await CreateIdentityAsync(countryId);
            cleanup.Add("identity", () => _client.Identities().DeleteAsync(identity.Id!));
            var address = new Address
            {
                CityName = "New York",
                PostalCode = "10001",
                AddressLine = "123 Main St",
                Identity = Identity.Build(identity.Id!),
                Country = Country.Build(countryId)
            };
            var created = (await _client.Addresses().CreateAsync(address)).Data;
            cleanup.Add("address", () => _client.Addresses().DeleteAsync(created.Id!));
            Console.WriteLine($"    address {created.Id}: {created.AddressLine}, {created.CityName}");
        });

        Section("Exports");
        // from and to must fall within the current month and the two previous ones
        var to = DateTime.UtcNow.Date;
        var from = to.AddDays(-14);
        await Check("Exports: CDR-In export", async () =>
        {
            var export = new Export
            {
                ExportType = ExportType.CdrIn,
                Filters = new Dictionary<string, object>
                {
                    { "from", from.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "to", to.ToString("yyyy-MM-dd HH:mm:ss") }
                }
            };
            var response = await _client.Exports().CreateAsync(export);
            Console.WriteLine($"    export {response.Data.Id}: {response.Data.Status}");
        });
        await Check("Exports: SMS-Out export, read again", async () =>
        {
            var filters = new Dictionary<string, object>
            {
                { "from", from.ToString("yyyy-MM-dd HH:mm:ss") }, // inclusive
                { "to", to.ToString("yyyy-MM-dd HH:mm:ss") },     // exclusive
                { "service_type", ExportServiceTypes.A2p }
            };
            // README: "sms-trunk-uuid"; the filter is left out when the account has no SMS trunk
            var smsTrunk = (await _client.SmsTrunks().ListAsync(new QueryParams().Page(1, 1))).Data.FirstOrDefault();
            if (smsTrunk != null)
                filters["sms_trunk.id"] = new List<string> { smsTrunk.Id! };
            var smsOut = (await _client.Exports().CreateAsync(new Export
            {
                ExportType = ExportType.SmsOut,
                Filters = filters
            })).Data;
            Expect(smsOut.ExportType == ExportType.SmsOut, $"export type is {smsOut.ExportType}");

            // The export is pending until its file is generated; read it again later
            var completed = (await _client.Exports().FindAsync(smsOut.Id!)).Data;
            Console.WriteLine($"    export {completed.Id}: {completed.Status}");
        });
        await Check("Exports: download a completed export", async () =>
        {
            var completed = (await _client.Exports().ListAsync(new QueryParams()
                .Filter("status", "completed")
                .Page(1, 1))).Data.FirstOrDefault(e => e.Url != null)
                ?? throw new SkipException("no completed export on the sandbox account yet");
            var dir = Directory.CreateTempSubdirectory("didww-readme-").FullName;
            try
            {
                var gz = Path.Combine(dir, "export.csv.gz");
                var csv = Path.Combine(dir, "export.csv");
                await _client.DownloadExportAsync(completed, gz);                // gzipped CSV, as served
                await _client.DownloadAndDecompressExportAsync(completed, csv);  // plain CSV
                await using (var stream = File.Create(Path.Combine(dir, "copy.csv.gz")))
                {
                    await _client.DownloadExportAsync(completed.Url!, stream);   // any writable Stream
                }
                Expect(new FileInfo(gz).Length > 0 && new FileInfo(csv).Length > 0, "an empty export file");
                Console.WriteLine($"    export {completed.Id}: {new FileInfo(gz).Length} bytes gzipped, {new FileInfo(csv).Length} bytes CSV");
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        });
    }

    // ─── Address Verifications, Emergency Services, DID History ─

    private static async Task VerificationsAndHistoryAsync()
    {
        Section("Address Verifications");
        await Check("Address verifications: list", async () =>
        {
            var verifications = await _client.AddressVerifications().ListAsync();
            Console.WriteLine($"    {verifications.Data.Count} address verification(s)");
        });
        await Check("Address verifications: create (left for review)", async cleanup =>
        {
            // A Ukrainian DID whose DID group has an address requirement, without an address verification
            var did = await DidNeedingAddressAsync();
            var requirement = (await _client.AddressRequirements().FindAsync(did.DidGroup!.AddressRequirement!.Id!,
                new QueryParams().Include("country", "personal_proof_types", "business_proof_types", "address_proof_types",
                    "personal_permanent_document", "business_permanent_document"))).Data;
            Console.WriteLine($"    DID {did.Number}: requirement {requirement.Id}, identity {requirement.IdentityType}, " +
                              $"address area level {requirement.AddressAreaLevel}");

            // An identity and an address that meet the requirement, checked before the verification
            var (identity, address) = await CreateIdentityAndAddressForAsync(requirement, did.DidGroup, cleanup);
            await _client.AddressRequirementValidations().CreateAsync(new AddressRequirementValidation
            {
                AddressRequirement = BaseResource.Build<AddressRequirement>(requirement.Id!),
                Address = Address.Build(address.Id!),
                Identity = Identity.Build(identity.Id!)
            });
            Console.WriteLine("    the identity and the address meet the requirement");

            var verification = new AddressVerification
            {
                CallbackUrl = "https://example.com/callback",
                CallbackMethod = CallbackMethod.Post,
                Address = Address.Build(address.Id!),
                Dids = new List<Did> { Did.Build(did.Id!) }
            };
            if (requirement.ServiceDescriptionRequired == true)
                verification.ServiceDescription = "Inbound calls to a customer support line";
            var result = await _client.AddressVerifications().CreateAsync(verification);
            // A verification cannot be deleted: it stays for DIDWW staff to approve or reject
            Console.WriteLine($"REVIEW address_verification {result.Data.Id} {result.Data.Reference}");
        });

        Section("Emergency Services");
        await Check("Emergency services: requirements and calling services", async () =>
        {
            var emergReqs = await _client.EmergencyRequirements().ListAsync();
            var emergServices = await _client.EmergencyCallingServices().ListAsync();
            Console.WriteLine($"    {emergReqs.Data.Count} requirement(s), {emergServices.Data.Count} calling service(s)");
        });
        await Check("Emergency services: create a verification (left for review)", async () =>
        {
            // The account's addresses with an identity, those in Ukraine first, the oldest first
            var addresses = (await _client.Addresses().ListAsync(new QueryParams()
                    .Include("country", "identity")
                    .Page(1, 50))).Data
                .Where(a => a.Country != null && a.Identity != null)
                .OrderBy(a => a.Country!.Iso == "UA" ? 0 : 1)
                .ThenBy(a => a.CreatedAt)
                .ToList();
            if (addresses.Count == 0)
                throw new InvalidOperationException("the sandbox account has no address with an identity");
            var country = addresses[0].Country!;

            // An emergency-capable DID in the country of the address, and its emergency requirement
            var did = await EmergencyDidAsync(country);
            var requirementParams = new QueryParams().Filter("country.id", country.Id!);
            if (did.DidGroup?.NumberType is { } numberType)
                requirementParams.Filter("number_type", EnumWire.Value(numberType));
            var requirement = (await _client.EmergencyRequirements().ListAsync(requirementParams)).Data.FirstOrDefault()
                              ?? throw new InvalidOperationException($"no emergency requirement for {country.Name} / {did.DidGroup?.NumberType}");
            var address = await EmergencyAddressAsync(requirement, addresses.Where(a => a.Country!.Id == country.Id).Take(10));
            Console.WriteLine($"    DID {did.Number}, address {address.Id} ({country.Name}), requirement {requirement.Id}");

            var emergVerification = new EmergencyVerification
            {
                CallbackUrl = "https://example.com/callback",
                CallbackMethod = CallbackMethod.Post,
                Address = Address.Build(address.Id!),
                Dids = new List<Did> { Did.Build(did.Id!) }
            };
            var emergResult = await _client.EmergencyVerifications().CreateAsync(emergVerification);
            // A verification cannot be deleted: it stays for DIDWW staff to approve or reject
            Console.WriteLine($"REVIEW emergency_verification {emergResult.Data.Id} {emergResult.Data.Reference}");
        });

        Section("DID History");
        await Check("DID history: list", async () =>
        {
            var history = await _client.DidHistory().ListAsync();
            foreach (var entry in history.Data.Take(3))
                Console.WriteLine($"    {entry.Action} {entry.CreatedAt}");
            Console.WriteLine($"    {history.Data.Count} event(s)");
        });
    }

    // ─── Prices ───────────────────────────────────────────────

    private static async Task PricesAsync()
    {
        Section("Prices");
        await Check("Price lookups", async () =>
        {
            var answered = 0;
            answered += await LookupAsync("Voice IN", () => _client.VoiceInPrices().LookupAsync(new Dictionary<string, string?>
            {
                ["number"] = "18005550123",
                ["src_country_iso"] = "US"
            }), price => $"{price.Destination} {price.SourceType}: {price.Rate} USD/min");
            answered += await LookupAsync("Voice OUT", () => _client.VoiceOutPrices().LookupAsync(
                new Dictionary<string, string?> { ["number"] = "493012345678" }), price => $"{price.Category} {price.Prefix}: {price.Rate}");
            answered += await LookupAsync("SMS OUT A2P", () => _client.SmsOutA2PPrices().LookupAsync(
                new Dictionary<string, string?> { ["dst_number"] = "4915123456789" }), price => $"{price.Country}: {price.Price}");
            answered += await LookupAsync("SMS IN", () => _client.SmsInPrices().LookupAsync(
                new Dictionary<string, string?> { ["dst_country_iso"] = "US" }), price => $"{price.Country}: {price.Price}");
            if (answered == 0)
                throw new SkipException("the sandbox account has no price plan");
        });
        await Check("Price files: fetch, read, head and download", async () =>
        {
            var dir = Directory.CreateTempSubdirectory("didww-readme-").FullName;
            try
            {
                var prices = _client.VoiceInPrices();
                var path = Path.Combine(dir, "voice_in_prices.csv.gz");

                var file = await prices.FetchAsync(path);
                Console.WriteLine(file.NotModified
                    ? "    answered 304, the file was kept"
                    : $"    downloaded {file.BytesWritten} bytes, last modified {file.LastModified:R}");

                await using (var stream = File.OpenRead(path))
                {
                    var rows = 0;
                    await foreach (var row in PriceFileReader.ReadRowsAsync(stream))
                    {
                        Console.WriteLine($"    {row["Destination"]}: {row["Rate"]}");
                        if (++rows == 3)
                            break;
                    }
                }

                var status = await prices.HeadAsync(file.LastModified); // sent as If-Modified-Since
                Console.WriteLine($"    HEAD: not modified {status.NotModified}, {status.ContentLength} bytes, {status.FileName}");

                file = await prices.DownloadToFileAsync(path);
                Expect(file.BytesWritten > 0, "DownloadToFileAsync wrote nothing");
                file = await prices.DownloadAsync(Stream.Null); // any writable Stream
                Expect(file.BytesWritten > 0, "DownloadAsync wrote nothing");
            }
            catch (DidwwPricesException e) when (e.Kind == DidwwPricesErrorKind.PricePlanNotAssigned)
            {
                throw new SkipException("the sandbox account has no Voice IN price plan");
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        });
    }

    // ─── Filtering, Dirty PATCH ───────────────────────────────

    private static async Task QueryAndDirtyPatchAsync()
    {
        Section("Filtering, Sorting, and Pagination");
        await Check("Filtering, sorting and pagination", async () =>
        {
            var us = (await _client.Countries().ListAsync(new QueryParams().Filter("iso", "US"))).Data.FirstOrDefault()
                     ?? throw new InvalidOperationException("country US not found");
            var queryParams = new QueryParams()
                .Filter("country.id", us.Id!)
                .Filter("name", "Arizona")
                .Include("country")
                .Sort("name")
                .Page(1, 25);
            var regions = await _client.Regions().ListAsync(queryParams);
            Expect(regions.Data.Count > 0, "no region named Arizona in the US");
            Expect(regions.Data.All(r => r.Name == "Arizona" && r.Country?.Iso == "US"), "the filters were not applied");
            Console.WriteLine($"    {regions.Data.Count} region(s): {regions.Data[0].Name}, {regions.Data[0].Country?.Name}");
        });

        Section("Dirty PATCH Serialization");
        await Check("Dirty PATCH: update a fetched DID (restored)", async cleanup =>
        {
            var original = await FirstDidAsync();
            cleanup.Add("DID", () => RestoreDidAsync(original));
            var did = (await _client.Dids().FindAsync(original.Id!)).Data;
            did.Description = "Updated description";
            did = (await _client.Dids().UpdateAsync(did)).Data;
            Expect(did.Description == "Updated description", "the description was not updated");
        });
        await Check("Dirty PATCH: build a resource for update", async cleanup =>
        {
            var created = await CreateTrunkAsync("README build trunk");
            cleanup.Add("trunk", () => _client.VoiceInTrunks().DeleteAsync(created.Id!));
            var trunk = VoiceInTrunk.Build(created.Id!);
            trunk.Name = $"New name {_suffix}";
            var updated = (await _client.VoiceInTrunks().UpdateAsync(trunk)).Data;
            Expect(updated.Name == trunk.Name, "the trunk was not renamed");
            Expect(updated.Configuration is SipConfiguration { Host: "sip.example.com" }, "the configuration changed");
        });
        await Check("Dirty PATCH: clear a field with explicit null (restored)", async cleanup =>
        {
            var original = await FirstDidAsync();
            cleanup.Add("DID", () => RestoreDidAsync(original));
            var did = Did.Build(original.Id!);
            did.Description = null;
            did = (await _client.Dids().UpdateAsync(did)).Data;
            Expect(string.IsNullOrEmpty(did.Description), "the description was not cleared");
        });
        await Check("Dirty PATCH: clear a relationship (restored)", async cleanup =>
        {
            var original = await FirstDidAsync();
            cleanup.Add("DID", () => RestoreDidAsync(original));
            var did = Did.Build(original.Id!);
            did.VoiceInTrunk = null;
            await _client.Dids().UpdateAsync(did);
            var after = (await _client.Dids().FindAsync(original.Id!, new QueryParams().Include("voice_in_trunk", "voice_in_trunk_group"))).Data;
            Expect(after.VoiceInTrunk == null && after.VoiceInTrunkGroup == null, "the DID is still routed");
        });
        await Check("Dirty PATCH: update an included resource (restored)", async cleanup =>
        {
            var original = await FirstDidAsync();
            var created = await CreateTrunkAsync("README included trunk");
            cleanup.Add("trunk", () => _client.VoiceInTrunks().DeleteAsync(created.Id!));
            cleanup.Add("DID", () => RestoreDidAsync(original));
            var assign = Did.Build(original.Id!);
            assign.VoiceInTrunk = VoiceInTrunk.Build(created.Id!);
            await _client.Dids().UpdateAsync(assign);

            var qp = new QueryParams().Include("voice_in_trunk");
            var did = (await _client.Dids().FindAsync(original.Id!, qp)).Data;
            var trunk = did.VoiceInTrunk ?? throw new InvalidOperationException("the trunk was not included");
            trunk.Name = $"Renamed trunk {_suffix}";
            var renamed = (await _client.VoiceInTrunks().UpdateAsync(trunk)).Data;
            Expect(renamed.Name == trunk.Name, "the included trunk was not renamed");
        });
    }

    // ─── Error Handling ───────────────────────────────────────

    private static async Task ErrorHandlingAsync()
    {
        Section("Error Handling");
        await Check("Error handling: DidwwApiException", async () =>
        {
            try
            {
                await _client.VoiceInTrunks().FindAsync("nonexistent");
                throw new InvalidOperationException("no error for an unknown trunk");
            }
            catch (DidwwApiException e)
            {
                Console.WriteLine($"    HTTP Status: {e.HttpStatus}");
                foreach (var error in e.Errors)
                    Console.WriteLine($"    Error: {error.Detail}");
                Expect(e.HttpStatus is >= 400 and < 500, $"HTTP status {e.HttpStatus}");
            }
        });
        await Check("Error handling: API rate limit", async () =>
        {
            try
            {
                var dids = await _client.Dids().ListAsync();
                Console.WriteLine($"    not rate limited ({dids.Data.Count} DIDs)");
            }
            catch (DidwwApiException e) when (e.HttpStatus == 429)
            {
                Console.WriteLine($"    API rate limit, retry after {e.RetryAfter}");
            }
        });
        await Check("Error handling: token service and prices errors by kind", async () =>
        {
            try
            {
                await _client.SmsOutA2PPrices().LookupAsync(new Dictionary<string, string?> { ["dst_number"] = "4915123456789" });
                Console.WriteLine("    price found");
            }
            catch (DidwwAuthException e) when (e.Kind == DidwwAuthErrorKind.RateLimited)
            {
                Console.WriteLine($"    Token service rate limit, retry after {e.RetryAfter}");
            }
            catch (DidwwPricesException e) when (e.Kind == DidwwPricesErrorKind.PriceNotFound)
            {
                Console.WriteLine("    No price for this number");
            }
            catch (DidwwPricesException e) when (e.Kind == DidwwPricesErrorKind.TooManyRequests)
            {
                Console.WriteLine($"    Prices rate limit, retry after {e.RetryAfter}");
            }
            catch (DidwwPricesException e) when (e.Kind == DidwwPricesErrorKind.PricePlanNotAssigned)
            {
                Console.WriteLine($"    {e.Kind}: {e.Message}");
            }
        });
    }

    // ─── File Encryption, Webhook, Dates ──────────────────────

    private static async Task EncryptionWebhookAndDatesAsync()
    {
        Section("File Encryption");
        await Check("File encryption and upload", async cleanup =>
        {
            var encrypt = new Encrypt(_client);
            byte[] fileData = SamplePdf.Create("README check"); // README: File.ReadAllBytes("document.pdf")
            byte[] encryptedData = encrypt.EncryptData(fileData);
            var fileId = await _client.UploadEncryptedFileAsync(
                encryptedData,
                "document.pdf",
                encrypt.Fingerprint,
                "My document"
            );
            cleanup.Add("encrypted file", () => _client.EncryptedFiles().DeleteAsync(fileId));
            Console.WriteLine($"    encrypted {fileData.Length} -> {encryptedData.Length} bytes, uploaded {fileId}");
        });

        Section("Webhook Signature Validation");
        await Check("Webhook signature validation", () =>
        {
            const string requestUrl = "https://example.com/webhooks/didww";
            var payloadParams = new Dictionary<string, string>
            {
                { "status", "completed" },
                { "id", "1dd7a68b-e235-402b-8912-fe73ee14243a" },
                { "type", "orders" }
            };
            var signature = WebhookExample.Sign("your-callback-secret", requestUrl, payloadParams);

            var validator = new RequestValidator("your-callback-secret");
            var isValid = validator.Validate(
                requestUrl,       // full original URL
                payloadParams,    // Dictionary<string, string> of payload key-value pairs
                signature         // value of X-DIDWW-Signature header
            );
            Expect(isValid, "a correctly signed callback did not validate");
            payloadParams["status"] = "canceled";
            Expect(!validator.Validate(requestUrl, payloadParams, signature), "a tampered callback validated");
            return Task.CompletedTask;
        });

        Section("Date and Datetime Fields");
        await Check("Date and datetime fields", async () =>
        {
            var dids = (await _client.Dids().ListAsync(new QueryParams().Page(1, 1))).Data;
            if (dids.Count > 0)
            {
                var did = (await _client.Dids().FindAsync(dids[0].Id!)).Data;
                Expect(did.CreatedAt != null, "the DID has no CreatedAt");
                Console.WriteLine($"    DID created {did.CreatedAt}, expires {did.ExpiresAt?.ToString() ?? "null"}");
            }
            var identities = (await _client.Identities().ListAsync(new QueryParams().Page(1, 1))).Data;
            if (identities.Count > 0)
            {
                var identity = (await _client.Identities().FindAsync(identities[0].Id!)).Data;
                Console.WriteLine($"    identity birth date {identity.BirthDate?.ToString("yyyy-MM-dd") ?? "null"}");
            }
            if (dids.Count == 0 && identities.Count == 0)
                throw new SkipException("no DID and no identity on the sandbox account");
        });
    }

    // ─── Shared steps ─────────────────────────────────────────

    // The first DID of the account that is not terminated, with its voice routing.
    private static async Task<Did> FirstDidAsync()
    {
        var dids = (await _client.Dids().ListAsync(new QueryParams()
            .Filter("terminated", "false")
            .Include("voice_in_trunk", "voice_in_trunk_group")
            .Page(1, 1))).Data;
        return dids.FirstOrDefault() ?? throw new SkipException("the sandbox account has no DID");
    }

    // The first DID that receives SMS, with its SMS routing.
    private static async Task<Did> FirstSmsDidAsync()
    {
        var dids = (await _client.Dids().ListAsync(new QueryParams()
            .Filter("terminated", "false")
            .Filter("did_group.features", EnumWire.Value(Feature.SmsIn))
            .Include("sms_trunk", "sms_trunk_group")
            .Page(1, 1))).Data;
        return dids.FirstOrDefault() ?? throw new SkipException("the sandbox account has no DID with inbound SMS");
    }

    // Puts back the description, capacity limit and voice routing of a DID.
    private static async Task RestoreDidAsync(Did original)
    {
        var restore = Did.Build(original.Id!);
        restore.Description = original.Description;
        restore.CapacityLimit = original.CapacityLimit;
        if (original.VoiceInTrunk != null)
            restore.VoiceInTrunk = VoiceInTrunk.Build(original.VoiceInTrunk.Id!);
        else if (original.VoiceInTrunkGroup != null)
            restore.VoiceInTrunkGroup = VoiceInTrunkGroup.Build(original.VoiceInTrunkGroup.Id!);
        else
            restore.VoiceInTrunk = null; // sends both voice_in_trunk and voice_in_trunk_group as null
        await _client.Dids().UpdateAsync(restore);
    }

    // Puts back the SMS routing of a DID.
    private static async Task RestoreSmsRoutingAsync(Did original)
    {
        var restore = Did.Build(original.Id!);
        if (original.SmsTrunk != null)
            restore.SmsTrunk = SmsTrunk.Build(original.SmsTrunk.Id!);
        else if (original.SmsTrunkGroup != null)
            restore.SmsTrunkGroup = SmsTrunkGroup.Build(original.SmsTrunkGroup.Id!);
        else
        {
            restore.SmsTrunk = null;
            restore.SmsTrunkGroup = null;
        }
        await _client.Dids().UpdateAsync(restore);
    }

    private static async Task ExpectSmsRoutingAsync(string didId, string? trunkId, string? groupId)
    {
        var did = (await _client.Dids().FindAsync(didId, new QueryParams().Include("sms_trunk", "sms_trunk_group"))).Data;
        Console.WriteLine($"    sms_trunk = {did.SmsTrunk?.Id ?? "null"}, sms_trunk_group = {did.SmsTrunkGroup?.Id ?? "null"}");
        Expect(did.SmsTrunk?.Id == trunkId && did.SmsTrunkGroup?.Id == groupId,
            $"expected sms_trunk {trunkId ?? "null"} and sms_trunk_group {groupId ?? "null"}");
    }

    private static async Task<VoiceInTrunk> CreateTrunkAsync(string name) =>
        (await _client.VoiceInTrunks().CreateAsync(new VoiceInTrunk
        {
            Name = $"{name} {_suffix}",
            Configuration = new SipConfiguration
            {
                Host = "sip.example.com",
                Port = 5060,
                Codecs = new List<Codec> { Codec.Pcmu, Codec.Pcma },
                TransportProtocolId = TransportProtocol.UDP
            }
        })).Data;

    private static async Task<SmsTrunk> CreateSmsTrunkAsync(string name) =>
        (await _client.SmsTrunks().CreateAsync(new SmsTrunk
        {
            Name = $"{name} {_suffix}",
            Configuration = new HttpInConfiguration
            {
                Url = "https://example.com/sms",
                HttpMethod = SmsHttpMethod.Get,
                QueryParameters = new Dictionary<string, object> { { "text", "{SMS_TEXT}" } }
            }
        })).Data;

    private static async Task<SmsTrunk> CreateHttpOutTrunkAsync(string name) =>
        (await _client.SmsTrunks().CreateAsync(new SmsTrunk
        {
            Name = $"{name} {_suffix}",
            Configuration = new HttpOutConfiguration
            {
                CallbackUrl = "https://example.com/sms/status",
                AllowedIps = new List<string> { "203.0.113.10" }
            }
        })).Data;

    private static async Task<string> FirstCountryIdAsync() =>
        (await _client.Countries().ListAsync(new QueryParams().Page(1, 1))).Data[0].Id!;

    private static async Task<Identity> CreateIdentityAsync(string countryId)
    {
        var identity = new Identity
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "12125551234",
            IdentityType = IdentityType.Personal,
            Country = Country.Build(countryId)
        };
        return (await _client.Identities().CreateAsync(identity)).Data;
    }

    // A Ukrainian DID of the account whose DID group has an address requirement and
    // which has no address verification yet; orders one when there is none.
    private static async Task<Did> DidNeedingAddressAsync()
    {
        var ukraine = (await _client.Countries().ListAsync(new QueryParams().Filter("iso", "UA"))).Data.FirstOrDefault()
                      ?? throw new InvalidOperationException("country UA not found");
        string[] didIncludes = ["did_group.address_requirement", "did_group.city", "address_verification"];
        var dids = (await _client.Dids().ListAsync(new QueryParams()
            .Filter("country.id", ukraine.Id!)
            .Filter("terminated", "false")
            .Include(didIncludes)
            .Page(1, 50))).Data;
        var did = dids.FirstOrDefault(d => d.DidGroup?.AddressRequirement != null && d.AddressVerification == null);
        if (did != null)
            return did;

        // The Kiev group has an address requirement; the other Ukrainian local groups come
        // after it. The requirement is in the response only with include=address_requirement.
        async Task<List<DidGroup>> LocalGroupsAsync(string? areaName)
        {
            var queryParams = new QueryParams()
                .Filter("country.id", ukraine.Id!)
                .Filter("number_type", EnumWire.Value(NumberType.Local))
                .Include("address_requirement", "stock_keeping_units")
                .Page(1, 50);
            if (areaName != null)
                queryParams.Filter("area_name", areaName);
            return (await _client.DidGroups().ListAsync(queryParams)).Data;
        }
        var groups = (await LocalGroupsAsync("Kiev")).Concat(await LocalGroupsAsync(null));
        foreach (var group in groups.Where(g => g.AddressRequirement != null && g.StockKeepingUnits is { Count: > 0 }))
        {
            var available = (await _client.AvailableDids().ListAsync(new QueryParams()
                .Filter("did_group.id", group.Id!)
                .Page(1, 1))).Data.FirstOrDefault();
            if (available == null)
                continue;
            var order = await OrderAndWaitAsync(new AvailableDidOrderItem
            {
                AvailableDidId = available.Id,
                SkuId = group.StockKeepingUnits![0].Id
            });
            return (await _client.Dids().ListAsync(new QueryParams()
                       .Filter("order.id", order.Id!)
                       .Include(didIncludes))).Data.FirstOrDefault()
                   ?? throw new InvalidOperationException($"order {order.Id} has no DID");
        }
        throw new InvalidOperationException("no Ukrainian local DID group with an address requirement and an available DID");
    }

    // An emergency-capable DID of the account in the country that is not bound to an
    // identity, an emergency verification or an emergency calling service yet (an
    // emergency verification binds the DID to the identity of its address); orders
    // one when there is none.
    private static async Task<Did> EmergencyDidAsync(Country country)
    {
        string[] didIncludes = ["did_group", "identity", "emergency_verification", "emergency_calling_service"];
        var dids = (await _client.Dids().ListAsync(new QueryParams()
            .Filter("did_group.features", EnumWire.Value(Feature.Emergency))
            .Filter("emergency_enabled", "false")
            .Filter("country.id", country.Id!)
            .Filter("terminated", "false")
            .Include(didIncludes)
            .Sort("-created_at")
            .Page(1, 50))).Data;
        var did = dids.FirstOrDefault(d =>
            d.Identity == null && d.EmergencyVerification == null && d.EmergencyCallingService == null);
        if (did != null)
            return did;

        var available = (await _client.AvailableDids().ListAsync(new QueryParams()
                .Filter("did_group.features", EnumWire.Value(Feature.Emergency))
                .Filter("country.id", country.Id!)
                .Include("did_group.stock_keeping_units")
                .Page(1, 10))).Data
            .FirstOrDefault(ad => ad.DidGroup?.StockKeepingUnits is { Count: > 0 })
            ?? throw new InvalidOperationException($"no available emergency-capable DID in {country.Name}");
        var order = await OrderAndWaitAsync(new AvailableDidOrderItem
        {
            AvailableDidId = available.Id,
            SkuId = available.DidGroup!.StockKeepingUnits![0].Id
        });
        return (await _client.Dids().ListAsync(new QueryParams()
                   .Filter("order.id", order.Id!)
                   .Include(didIncludes))).Data.FirstOrDefault()
               ?? throw new InvalidOperationException($"order {order.Id} has no DID");
    }

    // The first of the addresses that meets the emergency requirement with its identity.
    private static async Task<Address> EmergencyAddressAsync(EmergencyRequirement requirement, IEnumerable<Address> candidates)
    {
        var refusals = new List<string>();
        foreach (var address in candidates)
        {
            try
            {
                await _client.EmergencyRequirementValidations().CreateAsync(new EmergencyRequirementValidation
                {
                    EmergencyRequirement = EmergencyRequirement.Build(requirement.Id!),
                    Address = Address.Build(address.Id!),
                    Identity = Identity.Build(address.Identity!.Id!)
                });
                return address;
            }
            catch (DidwwApiException e) when (e.HttpStatus == 422)
            {
                refusals.Add($"{address.Id}: {Describe(e)}");
            }
        }
        throw new InvalidOperationException("no address of the account meets the emergency requirement: " +
                                            string.Join(" | ", refusals));
    }

    // Creates an identity and an address that meet an address requirement: the
    // identity type, the mandatory fields, the proofs and the permanent supporting
    // document it asks for. The cleanup deletes them if the API allows it.
    private static async Task<(Identity Identity, Address Address)> CreateIdentityAndAddressForAsync(
        AddressRequirement requirement, DidGroup didGroup, Cleanup cleanup)
    {
        var business = requirement.IdentityType == IdentityType.Business;
        var countryId = requirement.Country?.Id ?? throw new InvalidOperationException("the requirement has no country");
        var identity = new Identity
        {
            IdentityType = business ? IdentityType.Business : IdentityType.Personal,
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "380441234567",
            Country = Country.Build(countryId)
        };
        if (business)
            identity.CompanyName = "Example LLC";
        FillMandatoryFields(identity, business ? requirement.BusinessMandatoryFields : requirement.PersonalMandatoryFields);
        var createdIdentity = (await _client.Identities().CreateAsync(identity)).Data;
        cleanup.Add("identity", () => TryDeleteAsync("identity", () => _client.Identities().DeleteAsync(createdIdentity.Id!)));
        Console.WriteLine($"    identity {createdIdentity.Id} ({createdIdentity.IdentityType})");

        var address = new Address
        {
            CityName = didGroup.City?.Name ?? didGroup.AreaName ?? "Kyiv",
            PostalCode = "01001",
            AddressLine = "1 Khreshchatyk St",
            Description = "SDK README check",
            Identity = Identity.Build(createdIdentity.Id!),
            Country = Country.Build(countryId)
        };
        var createdAddress = (await _client.Addresses().CreateAsync(address)).Data;
        cleanup.Add("address", () => TryDeleteAsync("address", () => _client.Addresses().DeleteAsync(createdAddress.Id!)));
        Console.WriteLine($"    address {createdAddress.Id} ({createdAddress.CityName})");

        var encrypt = new Encrypt(_client);
        var pdf = SamplePdf.Create("Proof document");
        async Task<EncryptedFile> UploadAsync(string label) =>
            EncryptedFile.Build(await _client.UploadEncryptedFileAsync(
                encrypt.EncryptData(pdf), label + ".pdf.enc", encrypt.Fingerprint, label + ".pdf"));

        // One proof per required proof type: the requirement counts distinct types
        var identityProofQty = (business ? requirement.BusinessProofQty : requirement.PersonalProofQty) ?? 0;
        var identityProofTypes = (business ? requirement.BusinessProofTypes : requirement.PersonalProofTypes) ?? new List<ProofType>();
        foreach (var proofType in identityProofTypes.Take(identityProofQty))
            await CreateProofAsync(createdIdentity, proofType, await UploadAsync("identity_proof"));
        foreach (var proofType in (requirement.AddressProofTypes ?? new List<ProofType>()).Take(requirement.AddressProofQty ?? 0))
            await CreateProofAsync(createdAddress, proofType, await UploadAsync("address_proof"));

        var template = business ? requirement.BusinessPermanentDocument : requirement.PersonalPermanentDocument;
        if (template != null)
        {
            var document = (await _client.PermanentSupportingDocuments().CreateAsync(new PermanentSupportingDocument
            {
                Identity = Identity.Build(createdIdentity.Id!),
                Template = SupportingDocumentTemplate.Build(template.Id!),
                Files = new List<EncryptedFile> { await UploadAsync("permanent_document") }
            })).Data;
            Console.WriteLine($"    permanent supporting document {document.Id} ({template.Name})");
        }
        return (createdIdentity, createdAddress);
    }

    private static async Task CreateProofAsync(BaseResource entity, ProofType proofType, EncryptedFile file)
    {
        var proof = (await _client.Proofs().CreateAsync(new Proof
        {
            Entity = entity is Identity ? Identity.Build(entity.Id!) : Address.Build(entity.Id!),
            ProofType = ProofType.Build(proofType.Id!),
            Files = new List<EncryptedFile> { file }
        })).Data;
        Console.WriteLine($"    {(entity is Identity ? "identity" : "address")} proof {proof.Id} ({proofType.Name})");
    }

    // Fills the mandatory identity fields of a requirement, as the API names them.
    private static void FillMandatoryFields(Identity identity, IEnumerable<string>? fields)
    {
        foreach (var field in fields ?? [])
        {
            switch (field)
            {
                case "first_name" or "last_name" or "phone_number" or "country" or "company_name":
                    break; // already set
                case "birth_date":
                    identity.BirthDate = new DateOnly(1990, 5, 20);
                    break;
                case "id_number":
                    identity.IdNumber = "AB123456";
                    break;
                case "tax_id":
                    identity.VatId = "UA1234567890";
                    break;
                case "personal_tax_id":
                    identity.PersonalTaxId = "1234567890";
                    break;
                case "company_reg_number":
                    identity.CompanyRegNumber = "12345678";
                    break;
                case "company_website":
                    identity.CompanyWebsite = "https://example.com";
                    break;
                case "contact_email":
                    identity.ContactEmail = "john.doe@example.com";
                    break;
                case "description":
                    identity.Description = "SDK README check";
                    break;
                default:
                    Console.WriteLine($"    mandatory field {field} is not filled by the check");
                    break;
            }
        }
    }

    // Creates an order of one item and waits up to 120 s for it to complete.
    private static async Task<Order> OrderAndWaitAsync(OrderItemBase item)
    {
        var order = (await _client.Orders().CreateAsync(new Order { Items = new List<OrderItemBase> { item } })).Data;
        Console.WriteLine($"    ordered a DID: order {order.Id} ({order.Status})");
        for (var attempt = 0; attempt < 24 && order.IsPending; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
            order = (await _client.Orders().FindAsync(order.Id!)).Data;
        }
        Expect(order.IsCompleted, $"order {order.Id} is {order.Status}");
        return order;
    }

    private static async Task TryDeleteAsync(string what, Func<Task> delete)
    {
        try
        {
            await delete();
            Console.WriteLine($"    deleted the {what}");
        }
        catch (DidwwApiException e)
        {
            Console.WriteLine($"    left the {what} in place: {Describe(e)}");
        }
    }

    // The value of an environment variable a check needs; the check is skipped without it.
    private static string RequiredSetting(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(value) ? throw new SkipException($"set {name}") : value;
    }

    // redis://[user:password@]host[:port][/database] as a StackExchange.Redis configuration string
    private static string RedisConfiguration(string url)
    {
        var uri = new Uri(url);
        var parts = new List<string> { $"{uri.Host}:{(uri.Port > 0 ? uri.Port : 6379)}" };
        var database = uri.AbsolutePath.Trim('/');
        if (database.Length > 0)
            parts.Add($"defaultDatabase={database}");
        if (uri.UserInfo.Length > 0)
        {
            var userInfo = Uri.UnescapeDataString(uri.UserInfo);
            var colon = userInfo.IndexOf(':');
            if (colon > 0)
                parts.Add($"user={userInfo[..colon]}");
            parts.Add($"password={(colon >= 0 ? userInfo[(colon + 1)..] : userInfo)}");
        }
        if (uri.Scheme == "rediss")
            parts.Add("ssl=true");
        return string.Join(",", parts);
    }

    // Runs one price lookup; returns 1 when the service answered, 0 without a price plan.
    private static async Task<int> LookupAsync<TPrice>(string service, Func<Task<PriceLookup<TPrice>>> lookup,
        Func<TPrice, string> describe)
    {
        try
        {
            var result = await lookup();
            Console.WriteLine($"    {service}: {result.TotalRecords} record(s), {describe(result.Rows[0])}");
            return 1;
        }
        catch (DidwwPricesException e) when (e.Kind == DidwwPricesErrorKind.PricePlanNotAssigned)
        {
            Console.WriteLine($"    {service}: no price plan on the account");
            return 0;
        }
        catch (DidwwPricesException e) when (e.Kind == DidwwPricesErrorKind.PriceNotFound)
        {
            Console.WriteLine($"    {service}: no price for this filter");
            return 1;
        }
    }

    private static async Task DeleteIgnoringNotFoundAsync(Func<Task> delete)
    {
        try
        {
            await delete();
        }
        catch (DidwwApiException e) when (e.HttpStatus == 404)
        {
            // already deleted, e.g. together with its group
        }
    }

    // ─── Check runner ─────────────────────────────────────────

    private static void Section(string name) => Console.WriteLine($"\n== {name} ==");

    private static Task Check(string name, Func<Task> body) => Check(name, _ => body());

    // Runs one check. Cleanup actions registered by the check run afterwards in
    // reverse order, also when it fails; a cleanup error fails the check too.
    private static async Task Check(string name, Func<Cleanup, Task> body)
    {
        var cleanup = new Cleanup();
        System.Exception? failure = null;
        try
        {
            await body(cleanup);
        }
        catch (System.Exception e)
        {
            failure = e;
        }
        var cleanupErrors = await cleanup.RunAsync();
        if (failure == null && cleanupErrors.Count > 0)
            failure = new InvalidOperationException("cleanup failed: " + string.Join("; ", cleanupErrors));
        else if (cleanupErrors.Count > 0)
            Console.WriteLine($"    cleanup failed too: {string.Join("; ", cleanupErrors)}");

        switch (failure)
        {
            case null:
                _passed++;
                Console.WriteLine($"  [PASS] {name}");
                break;
            case SkipException skip:
                Skip(name, skip.Message);
                break;
            default:
                _failed++;
                Failures.Add($"{name}: {Describe(failure)}");
                Console.WriteLine($"  [FAIL] {name}: {Describe(failure)}");
                break;
        }
    }

    private static void Skip(string name, string reason)
    {
        Skipped.Add($"{name}: {reason}");
        Console.WriteLine($"  [SKIP] {name}: {reason}");
    }

    private static void Expect(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static string Describe(System.Exception e) => e switch
    {
        DidwwApiException api when api.Errors.Count > 0 =>
            $"{api.GetType().Name} {api.HttpStatus}: {string.Join("; ", api.Errors.Select(error => error.Detail ?? error.Title))}",
        DidwwApiException api => $"{api.GetType().Name} {api.HttpStatus}: {api.Message}",
        _ => $"{e.GetType().Name}: {e.Message}"
    };

    private static void PrintSummary()
    {
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine($"Results: {_passed} passed, {_failed} failed");
        if (Skipped.Count > 0)
        {
            Console.WriteLine($"\nSkipped ({Skipped.Count}):");
            foreach (var skipped in Skipped)
                Console.WriteLine($"  - {skipped}");
        }
        if (Failures.Count > 0)
        {
            Console.WriteLine("\nFailures:");
            foreach (var failure in Failures)
                Console.WriteLine($"  - {failure}");
        }
        Console.WriteLine(new string('=', 50));
    }

    private sealed class SkipException(string reason) : System.Exception(reason);

    private sealed class Cleanup
    {
        private readonly Stack<(string What, Func<Task> Action)> _actions = new();

        public void Add(string what, Func<Task> action) => _actions.Push((what, action));

        public async Task<List<string>> RunAsync()
        {
            var errors = new List<string>();
            while (_actions.Count > 0)
            {
                var (what, action) = _actions.Pop();
                try
                {
                    await action();
                }
                catch (System.Exception e)
                {
                    errors.Add($"{what}: {Describe(e)}");
                }
            }
            return errors;
        }
    }

    // An ILogger that keeps the formatted messages.
    private sealed class RecordingLogger : ILogger
    {
        public List<string> Lines { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception,
            Func<TState, System.Exception?, string> formatter) =>
            Lines.Add($"{logLevel}: {formatter(state, exception)}");
    }

    // A WebProxy that notes the hosts the handler asks it to proxy.
    private sealed class RecordingProxy(string proxyUrl) : IWebProxy
    {
        private readonly WebProxy _proxy = new(proxyUrl);

        public ConcurrentDictionary<string, int> Hosts { get; } = new();

        public ICredentials? Credentials
        {
            get => _proxy.Credentials;
            set => _proxy.Credentials = value;
        }

        public Uri? GetProxy(Uri destination)
        {
            Hosts.AddOrUpdate(destination.Host, 1, (_, count) => count + 1);
            return _proxy.GetProxy(destination);
        }

        public bool IsBypassed(Uri host) => _proxy.IsBypassed(host);
    }

    // Wraps a token store and counts the tokens read from it and written to it.
    private sealed class CountingTokenStore(ITokenStore inner) : ITokenStore
    {
        public int Hits { get; private set; }

        public List<string> Writes { get; } = new();

        public async ValueTask<DidwwToken?> GetAsync(string key, CancellationToken cancellationToken)
        {
            var token = await inner.GetAsync(key, cancellationToken);
            if (token != null)
                Hits++;
            return token;
        }

        public ValueTask SetAsync(string key, DidwwToken token, TimeSpan ttl, CancellationToken cancellationToken)
        {
            Writes.Add(key);
            return inner.SetAsync(key, token, ttl, cancellationToken);
        }

        public ValueTask DeleteAsync(string key, CancellationToken cancellationToken) =>
            inner.DeleteAsync(key, cancellationToken);
    }

    // An ITokenStore of your own, as in the README.
    private sealed class DictionaryTokenStore : ITokenStore
    {
        public ConcurrentDictionary<string, DidwwToken> Tokens { get; } = new();

        public ValueTask<DidwwToken?> GetAsync(string key, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Tokens.TryGetValue(key, out var token) ? token : null);

        public ValueTask SetAsync(string key, DidwwToken token, TimeSpan ttl, CancellationToken cancellationToken)
        {
            Tokens[key] = token;
            return ValueTask.CompletedTask;
        }

        public ValueTask DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Tokens.TryRemove(key, out _);
            return ValueTask.CompletedTask;
        }
    }
}
