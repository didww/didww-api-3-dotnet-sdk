using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// Exclusive trunk / trunk group assignment on a DID: assigning a trunk sends
// the trunk group as null in the same PATCH, and the other way round.
// At the end the DID's original voice routing and description are restored
// and the created trunk and trunk group are deleted, also when a step fails.
public static class DidTrunkAssignmentExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        var didParams = new QueryParams()
            .Include("voice_in_trunk", "voice_in_trunk_group")
            .Page(1, 1);
        var dids = (await client.Dids().ListAsync(didParams)).Data;
        if (dids.Count == 0)
        {
            Console.WriteLine("No DIDs found. Order a DID first.");
            return;
        }
        var did = dids[0];
        Console.WriteLine($"Using DID: {did.Number} ({did.Id})");

        // Remember the current routing and description to put them back at the end
        var originalTrunkId = did.VoiceInTrunk?.Id;
        var originalGroupId = did.VoiceInTrunkGroup?.Id;
        var originalDescription = did.Description;

        var suffix = Guid.NewGuid().ToString("N")[..8];
        string? trunkId = null;
        string? groupId = null;
        var didChanged = false;
        try
        {
            trunkId = (await client.VoiceInTrunks().CreateAsync(new VoiceInTrunk
            {
                Name = "Assignment Trunk " + suffix,
                Configuration = new SipConfiguration
                {
                    Host = "sip.example.com",
                    Port = 5060,
                    Codecs = new List<Codec> { Codec.Pcmu, Codec.Pcma },
                    TransportProtocolId = TransportProtocol.UDP
                },
                Pop = Pop.Fra // optional; leave it unset to let DIDWW choose
            })).Data.Id!;
            Console.WriteLine($"Created trunk: {trunkId}");

            groupId = (await client.VoiceInTrunkGroups().CreateAsync(new VoiceInTrunkGroup
            {
                Name = "Assignment Group " + suffix,
                CapacityLimit = 10
            })).Data.Id!;
            Console.WriteLine($"Created trunk group: {groupId}");

            // 1. Assign the trunk (sends voice_in_trunk_group as null)
            didChanged = true;
            var update1 = Did.Build(did.Id!);
            update1.VoiceInTrunk = VoiceInTrunk.Build(trunkId);
            await client.Dids().UpdateAsync(update1);
            Console.WriteLine("\n1. Assigned trunk:");
            await PrintAssignmentAsync(client, did.Id!);

            // 2. Assign the trunk group (sends voice_in_trunk as null)
            var update2 = Did.Build(did.Id!);
            update2.VoiceInTrunkGroup = VoiceInTrunkGroup.Build(groupId);
            await client.Dids().UpdateAsync(update2);
            Console.WriteLine("\n2. Assigned trunk group (the trunk is cleared):");
            await PrintAssignmentAsync(client, did.Id!);

            // 3. Re-assign the trunk (clears the trunk group again)
            var update3 = Did.Build(did.Id!);
            update3.VoiceInTrunk = VoiceInTrunk.Build(trunkId);
            await client.Dids().UpdateAsync(update3);
            Console.WriteLine("\n3. Re-assigned trunk:");
            await PrintAssignmentAsync(client, did.Id!);

            // 4. Update the description only: only changed fields are sent, so the trunk stays
            var update4 = Did.Build(did.Id!);
            update4.Description = "DID with trunk assigned";
            await client.Dids().UpdateAsync(update4);
            Console.WriteLine("\n4. Updated description only (trunk stays):");
            await PrintAssignmentAsync(client, did.Id!);
        }
        finally
        {
            try
            {
                // Restore the DID first, so the created trunk and group are no longer in use
                if (didChanged)
                {
                    var restore = Did.Build(did.Id!);
                    if (originalTrunkId != null)
                        restore.VoiceInTrunk = VoiceInTrunk.Build(originalTrunkId);
                    else if (originalGroupId != null)
                        restore.VoiceInTrunkGroup = VoiceInTrunkGroup.Build(originalGroupId);
                    else
                        restore.VoiceInTrunk = null; // sends both voice_in_trunk and voice_in_trunk_group as null
                    restore.Description = originalDescription;
                    await client.Dids().UpdateAsync(restore);
                    Console.WriteLine("\nRestored the DID's original routing:");
                    await PrintAssignmentAsync(client, did.Id!);
                }
            }
            finally
            {
                // Delete the trunk and the group even when the restore or its read-back fails
                await DeleteCreatedAsync(client, trunkId, groupId);
            }
        }
    }

    private static async Task DeleteCreatedAsync(DidwwClient client, string? trunkId, string? groupId)
    {
        try
        {
            if (trunkId != null)
            {
                await client.VoiceInTrunks().DeleteAsync(trunkId);
                Console.WriteLine("Deleted trunk");
            }
        }
        finally
        {
            if (groupId != null)
            {
                await client.VoiceInTrunkGroups().DeleteAsync(groupId);
                Console.WriteLine("Deleted trunk group");
            }
        }
    }

    private static async Task PrintAssignmentAsync(DidwwClient client, string didId)
    {
        var queryParams = new QueryParams().Include("voice_in_trunk", "voice_in_trunk_group");
        var result = (await client.Dids().FindAsync(didId, queryParams)).Data;
        Console.WriteLine($"   trunk = {result.VoiceInTrunk?.Id ?? "null"}");
        Console.WriteLine($"   group = {result.VoiceInTrunkGroup?.Id ?? "null"}");
    }
}
