using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

public static class TrunkGroupsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        var suffix = Guid.NewGuid().ToString()[..8];

        Console.WriteLine("--- Create Two Trunks ---");
        var sipA = new SipConfiguration
        {
            Host = "sip-a.example.com",
            Port = 5060,
            CodecIds = new List<Codec> { Codec.PCMU, Codec.PCMA },
            TransportProtocolId = TransportProtocol.UDP
        };
        var trunkA = new VoiceInTrunk { Name = "Group Trunk A " + suffix, Configuration = sipA };
        trunkA = (await client.VoiceInTrunks().CreateAsync(trunkA)).Data;
        Console.WriteLine($"  Created trunk A: {trunkA.Id}");

        var sipB = new SipConfiguration
        {
            Host = "sip-b.example.com",
            Port = 5060,
            CodecIds = new List<Codec> { Codec.PCMU },
            TransportProtocolId = TransportProtocol.UDP
        };
        var trunkB = new VoiceInTrunk { Name = "Group Trunk B " + suffix, Configuration = sipB };
        trunkB = (await client.VoiceInTrunks().CreateAsync(trunkB)).Data;
        Console.WriteLine($"  Created trunk B: {trunkB.Id}");

        Console.WriteLine("\n--- Create Trunk Group ---");
        var group = new VoiceInTrunkGroup
        {
            Name = "My Trunk Group " + suffix,
            CapacityLimit = 10,
            VoiceInTrunks = new List<VoiceInTrunk>
            {
                VoiceInTrunk.Build(trunkA.Id!),
                VoiceInTrunk.Build(trunkB.Id!)
            }
        };
        group = (await client.VoiceInTrunkGroups().CreateAsync(group)).Data;
        Console.WriteLine($"  Created group: {group.Id} - {group.Name}");

        Console.WriteLine("\n--- List Trunk Groups ---");
        var queryParams = new QueryParams().Include("voice_in_trunks");
        var listResponse = await client.VoiceInTrunkGroups().ListAsync(queryParams);
        foreach (var g in listResponse.Data)
        {
            var trunkCount = g.VoiceInTrunks?.Count ?? 0;
            Console.WriteLine($"  {g.Name} ({trunkCount} trunks)");
        }

        Console.WriteLine("\n--- Update Trunk Group ---");
        group.Name = "Updated Group " + suffix;
        var updated = (await client.VoiceInTrunkGroups().UpdateAsync(group)).Data;
        Console.WriteLine($"  Updated name: {updated.Name}");

        Console.WriteLine("\n--- Delete Trunk Group ---");
        await client.VoiceInTrunkGroups().DeleteAsync(group.Id!);
        Console.WriteLine($"  Deleted group (trunks cascade-deleted)");
    }
}
