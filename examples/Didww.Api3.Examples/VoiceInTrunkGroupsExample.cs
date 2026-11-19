using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// CRUD for Voice IN trunk groups with their trunks: creates two SIP trunks,
// puts them in a new group, lists the groups, renames the group and deletes
// it. Deleting the group also deletes the trunks in it; the trunks and the
// group are deleted also when a step fails.
public static class VoiceInTrunkGroupsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var trunkIds = new List<string>();
        string? groupId = null;

        try
        {
            Console.WriteLine("--- Create Two Trunks ---");
            var trunkA = (await client.VoiceInTrunks().CreateAsync(new VoiceInTrunk
            {
                Name = "Group Trunk A " + suffix,
                Configuration = new SipConfiguration
                {
                    Host = "sip-a.example.com",
                    Port = 5060,
                    Codecs = new List<Codec> { Codec.Pcmu, Codec.Pcma },
                    TransportProtocolId = TransportProtocol.UDP
                },
                Pop = Pop.Fra // optional; leave it unset to let DIDWW choose
            })).Data;
            trunkIds.Add(trunkA.Id!);
            Console.WriteLine($"  Created trunk A: {trunkA.Id}");

            var trunkB = (await client.VoiceInTrunks().CreateAsync(new VoiceInTrunk
            {
                Name = "Group Trunk B " + suffix,
                Configuration = new SipConfiguration
                {
                    Host = "sip-b.example.com",
                    Port = 5060,
                    Codecs = new List<Codec> { Codec.Pcmu },
                    TransportProtocolId = TransportProtocol.UDP
                }
            })).Data;
            trunkIds.Add(trunkB.Id!);
            Console.WriteLine($"  Created trunk B: {trunkB.Id}");

            // Create a trunk group with both trunks (2026-04-16 external_reference_id for customer tagging)
            Console.WriteLine("\n--- Create Trunk Group ---");
            var group = (await client.VoiceInTrunkGroups().CreateAsync(new VoiceInTrunkGroup
            {
                Name = "My Trunk Group " + suffix,
                CapacityLimit = 10,
                ExternalReferenceId = "dotnet-tg-" + suffix,
                VoiceInTrunks = new List<VoiceInTrunk>
                {
                    VoiceInTrunk.Build(trunkA.Id!),
                    VoiceInTrunk.Build(trunkB.Id!)
                }
            })).Data;
            groupId = group.Id;
            Console.WriteLine($"  Created group: {group.Id} - {group.Name}");
            Console.WriteLine($"    External reference: {group.ExternalReferenceId}");

            Console.WriteLine("\n--- List Trunk Groups ---");
            var groups = (await client.VoiceInTrunkGroups().ListAsync(new QueryParams().Include("voice_in_trunks"))).Data;
            Console.WriteLine($"  Found {groups.Count} trunk groups");
            foreach (var g in groups)
            {
                var trunkCount = g.VoiceInTrunks?.Count ?? 0;
                Console.WriteLine($"  {g.Name} ({trunkCount} trunks)");
                if (g.ExternalReferenceId != null)
                    Console.WriteLine($"    External reference: {g.ExternalReferenceId}");
            }

            Console.WriteLine("\n--- Rename Trunk Group ---");
            var rename = VoiceInTrunkGroup.Build(group.Id!);
            rename.Name = "Updated Group " + suffix;
            var updated = (await client.VoiceInTrunkGroups().UpdateAsync(rename)).Data;
            Console.WriteLine($"  Updated name: {updated.Name}");
        }
        finally
        {
            if (groupId != null)
            {
                Console.WriteLine("\n--- Delete Trunk Group ---");
                await client.VoiceInTrunkGroups().DeleteAsync(groupId);
                Console.WriteLine("  Deleted trunk group (cascaded to trunks)");
            }
            else
            {
                // Without a group the trunks are deleted one by one, each also when another fails
                await CleanupSteps.RunAllAsync(trunkIds.Select(trunkId => (Func<Task>)(async () =>
                {
                    await client.VoiceInTrunks().DeleteAsync(trunkId);
                    Console.WriteLine($"  Deleted trunk {trunkId}");
                })));
            }
        }
    }
}
