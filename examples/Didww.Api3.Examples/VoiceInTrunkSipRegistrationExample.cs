using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

/// <summary>
/// End-to-end SIP registration flow on /voice_in_trunks (API 2026-04-16):
/// create with sip_registration enabled → rename → disable by setting Host
/// → re-enable by toggling the flag. Demonstrates how the SDK keeps the
/// dependent fields (Host, Port, UseDidInRuri) aligned with the server's
/// validation rules. The sandbox trunk is left in place after the script
/// completes.
/// </summary>
public static class VoiceInTrunkSipRegistrationExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("=== .NET SDK — SIP registration flow ===");

        // 1) Create with sip_registration enabled.
        Console.WriteLine("\n[1/4] Create with sip_registration enabled...");
        var sip = new SipConfiguration
        {
            EnabledSipRegistration = true,
            UseDidInRuri = true,
            CnamLookup = false,
            Codecs = new List<Codec> { Codec.Pcmu, Codec.Pcma },
            TransportProtocolId = TransportProtocol.UDP,
        };
        var trunk = new VoiceInTrunk
        {
            Name = $"dotnet-sip-registration-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
            Priority = 1,
            Weight = 100,
            CliFormat = CliFormat.E164,
            RingingTimeout = 30,
            Configuration = sip,
        };
        var created = (await client.VoiceInTrunks().CreateAsync(trunk)).Data;
        var trunkId = created.Id!;
        var cfg1 = (SipConfiguration)created.Configuration!;
        Console.WriteLine($"  id={trunkId}");
        Console.WriteLine($"  IncomingAuthUsername={cfg1.IncomingAuthUsername}");
        Console.WriteLine($"  IncomingAuthPassword={cfg1.IncomingAuthPassword}");

        // 2) Rename — single-field PATCH.
        Console.WriteLine("\n[2/4] Rename trunk...");
        var rename = VoiceInTrunk.Build(trunkId);
        rename.Name = $"dotnet-renamed-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        await client.VoiceInTrunks().UpdateAsync(rename);
        Console.WriteLine($"  name={rename.Name}");

        // 3) Disable sip_registration by setting Host.
        Console.WriteLine("\n[3/4] Disable by setting Host...");
        var disable = VoiceInTrunk.Build(trunkId);
        disable.Configuration = new SipConfiguration { Host = "203.0.113.10" };
        await client.VoiceInTrunks().UpdateAsync(disable);
        var fresh3 = (await client.VoiceInTrunks().FindAsync(trunkId)).Data;
        var cfg3 = (SipConfiguration)fresh3.Configuration!;
        Console.WriteLine($"  EnabledSipRegistration={cfg3.EnabledSipRegistration}");
        Console.WriteLine($"  UseDidInRuri={cfg3.UseDidInRuri}");
        Console.WriteLine($"  Host={cfg3.Host}");
        Console.WriteLine($"  IncomingAuthUsername={cfg3.IncomingAuthUsername ?? "null"}");

        // 4) Re-enable sip_registration. The SDK should send Host=null / Port=null
        //    on the wire so the server clears the values it had persisted.
        Console.WriteLine("\n[4/4] Re-enable by toggling EnabledSipRegistration...");
        var reEnable = VoiceInTrunk.Build(trunkId);
        reEnable.Configuration = new SipConfiguration
        {
            EnabledSipRegistration = true,
            UseDidInRuri = true,
        };
        try
        {
            await client.VoiceInTrunks().UpdateAsync(reEnable);
            var fresh4 = (await client.VoiceInTrunks().FindAsync(trunkId)).Data;
            var cfg4 = (SipConfiguration)fresh4.Configuration!;
            Console.WriteLine($"  EnabledSipRegistration={cfg4.EnabledSipRegistration}");
            Console.WriteLine($"  Host={cfg4.Host ?? "null"}");
            Console.WriteLine($"  IncomingAuthUsername={cfg4.IncomingAuthUsername}");
            Console.WriteLine($"\n=== PASS — trunk {trunkId} left in sandbox ===");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"  ✗ FAIL: {ex.Message}");
            Console.WriteLine($"\n=== FAIL at re-enable — trunk {trunkId} left in sandbox ===");
        }
    }
}
