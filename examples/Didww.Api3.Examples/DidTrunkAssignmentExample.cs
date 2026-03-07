using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

public static class DidTrunkAssignmentExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Create Voice In Trunk ---");
        var sipConfig = new SipConfiguration
        {
            Username = "example-user",
            Host = "192.168.1.100",
            Port = 5060,
            CodecIds = new List<Codec> { Codec.PCMU, Codec.PCMA }
        };

        var trunk = new VoiceInTrunk
        {
            Name = "SDK Example Trunk",
            Configuration = sipConfig
        };

        var trunkResponse = await client.VoiceInTrunks().CreateAsync(trunk);
        var createdTrunk = trunkResponse.Data;
        Console.WriteLine($"  Trunk created: {createdTrunk.Id} ({createdTrunk.Name})");

        Console.WriteLine("\n--- Assign Trunk to DID ---");
        var dids = await client.Dids().ListAsync(new QueryParams().Page(1, 1));
        if (dids.Data.Count == 0)
        {
            Console.WriteLine("  No DIDs found to assign, cleaning up trunk.");
            await client.VoiceInTrunks().DeleteAsync(createdTrunk.Id!);
            return;
        }

        var did = dids.Data[0];
        Console.WriteLine($"  DID: {did.Number} ({did.Id})");

        // Assign trunk to DID (setting trunk nullifies trunk_group)
        var didToUpdate = Did.Build(did.Id!);
        didToUpdate.VoiceInTrunk = VoiceInTrunk.Build(createdTrunk.Id!);

        var updatedDid = (await client.Dids().UpdateAsync(didToUpdate)).Data;
        Console.WriteLine($"  Trunk assigned to DID {updatedDid.Number}");

        Console.WriteLine("\n--- Verify Exclusivity ---");
        Console.WriteLine("  Setting VoiceInTrunk nullifies VoiceInTrunkGroup and vice versa.");

        Console.WriteLine("\n--- Cleanup ---");
        // Remove trunk assignment
        var resetDid = Did.Build(did.Id!);
        resetDid.VoiceInTrunk = null;
        await client.Dids().UpdateAsync(resetDid);
        Console.WriteLine($"  Trunk unassigned from DID.");

        await client.VoiceInTrunks().DeleteAsync(createdTrunk.Id!);
        Console.WriteLine($"  Trunk deleted: {createdTrunk.Id}");
    }
}
