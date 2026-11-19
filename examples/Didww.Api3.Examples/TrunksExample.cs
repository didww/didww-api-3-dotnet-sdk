using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// Lists Voice IN trunks with their trunk group, finds one, creates a SIP trunk
// and a PSTN trunk, updates both and deletes both, also when a step fails.
public static class TrunksExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- List Voice In Trunks ---");
        var listParams = new QueryParams()
            .Include("voice_in_trunk_group")
            .Page(1, 10);
        var trunks = (await client.VoiceInTrunks().ListAsync(listParams)).Data;
        Console.WriteLine($"  Found {trunks.Count} trunks");
        foreach (var t in trunks)
        {
            var group = t.VoiceInTrunkGroup?.Name ?? "no group";
            Console.WriteLine($"  {t.Name} [{t.Configuration?.ConfigurationType}] pop={t.Pop?.ToString() ?? "auto"}, {group}");
        }

        if (trunks.Count > 0)
        {
            Console.WriteLine("\n--- Find Voice In Trunk ---");
            var found = (await client.VoiceInTrunks().FindAsync(trunks[0].Id!, new QueryParams().Include("voice_in_trunk_group"))).Data;
            Console.WriteLine($"  {found.Name} (ID: {found.Id}), created {found.CreatedAt}");
            if (found.Configuration is SipConfiguration foundSip)
                Console.WriteLine($"    SIP host={foundSip.Host} codecs={string.Join(", ", foundSip.Codecs ?? new List<Codec>())}");
            else if (found.Configuration is PstnConfiguration foundPstn)
                Console.WriteLine($"    PSTN dst={foundPstn.Dst}");
        }

        var suffix = Guid.NewGuid().ToString("N")[..8];
        string? sipId = null;
        string? pstnId = null;
        try
        {
            Console.WriteLine("\n--- Create SIP Voice In Trunk ---");
            var sip = (await client.VoiceInTrunks().CreateAsync(new VoiceInTrunk
            {
                Name = "My SIP Trunk " + suffix,
                Priority = 1,
                Weight = 100,
                CliFormat = CliFormat.E164,
                RingingTimeout = 30,
                CapacityLimit = 10,
                Configuration = new SipConfiguration
                {
                    Username = "myuser",
                    Host = "sip.example.com",
                    Port = 5060,
                    // Codecs in priority order: the first has the highest priority.
                    Codecs = new List<Codec> { Codec.Pcmu, Codec.Pcma, Codec.TelephoneEvent },
                    IceMode = IceMode.Accept,
                    TransportProtocolId = TransportProtocol.UDP,
                    ReroutingDisconnectCodeIds = new List<ReroutingDisconnectCode>
                    {
                        ReroutingDisconnectCode.SIP_480_TEMPORARILY_UNAVAILABLE,
                        ReroutingDisconnectCode.SIP_503_SERVICE_UNAVAILABLE
                    }
                },
                Pop = Pop.Fra // optional; leave it unset to let DIDWW choose
            })).Data;
            sipId = sip.Id;
            Console.WriteLine($"  SIP trunk created: {sip.Id} ({sip.Name}, pop {sip.Pop})");

            Console.WriteLine("\n--- Create PSTN Voice In Trunk ---");
            var pstn = (await client.VoiceInTrunks().CreateAsync(new VoiceInTrunk
            {
                Name = "My PSTN Trunk " + suffix,
                RingingTimeout = 30,
                Configuration = new PstnConfiguration { Dst = "12125551234" }
            })).Data;
            pstnId = pstn.Id;
            Console.WriteLine($"  PSTN trunk created: {pstn.Id} ({pstn.Name}, dst {(pstn.Configuration as PstnConfiguration)?.Dst})");

            Console.WriteLine("\n--- Update SIP Voice In Trunk ---");
            var sipUpdate = VoiceInTrunk.Build(sip.Id!);
            sipUpdate.Description = "Updated description";
            sipUpdate.CapacityLimit = 20;
            var sipUpdated = (await client.VoiceInTrunks().UpdateAsync(sipUpdate)).Data;
            Console.WriteLine($"  {sipUpdated.Name}: description={sipUpdated.Description}, capacity_limit={sipUpdated.CapacityLimit}");

            Console.WriteLine("\n--- Update PSTN Voice In Trunk ---");
            var pstnUpdate = VoiceInTrunk.Build(pstn.Id!);
            pstnUpdate.Name = "My PSTN Trunk " + suffix + " (updated)";
            pstnUpdate.Configuration = new PstnConfiguration { Dst = "12125559876" };
            var pstnUpdated = (await client.VoiceInTrunks().UpdateAsync(pstnUpdate)).Data;
            Console.WriteLine($"  {pstnUpdated.Name}: dst={(pstnUpdated.Configuration as PstnConfiguration)?.Dst}");
        }
        finally
        {
            Console.WriteLine("\n--- Delete Voice In Trunks ---");
            if (sipId != null)
            {
                await client.VoiceInTrunks().DeleteAsync(sipId);
                Console.WriteLine($"  SIP trunk deleted: {sipId}");
            }
            if (pstnId != null)
            {
                await client.VoiceInTrunks().DeleteAsync(pstnId);
                Console.WriteLine($"  PSTN trunk deleted: {pstnId}");
            }
        }
    }
}
