using Didww.Api3.Http;
using Didww.Api3.Resource;

namespace Didww.Api3.Examples;

// Routes a DID's inbound SMS (2026-10-22) to an SMS trunk, then to an SMS
// trunk group. The two are exclusive: assigning one sends the other as null in
// the same PATCH, just like VoiceInTrunk and VoiceInTrunkGroup. At the end the
// DID's original SMS routing is restored.
public static class DidSmsRoutingExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        var did = await FindDidAsync(client);
        if (did == null)
            return;

        var trunk = await FindInboundTrunkAsync(client);
        if (trunk == null)
            return;

        var group = await FindTrunkGroupAsync(client);

        // Remember the current routing to put it back at the end.
        var originalTrunkId = did.SmsTrunk?.Id;
        var originalGroupId = did.SmsTrunkGroup?.Id;
        Console.WriteLine("Current SMS routing:");
        await PrintSmsRoutingAsync(client, did.Id!);

        try
        {
            // 1. Assign the SMS trunk (sends sms_trunk_group as null)
            Console.WriteLine("\n=== 1. Assigning SMS trunk to DID ===");
            var assignTrunk = Did.Build(did.Id!);
            assignTrunk.SmsTrunk = SmsTrunk.Build(trunk.Id!);
            await client.Dids().UpdateAsync(assignTrunk);
            await PrintSmsRoutingAsync(client, did.Id!);
            await PrintRoutedDidsAsync(client, trunk);

            // 2. Assign the SMS trunk group (sends sms_trunk as null)
            if (group != null)
            {
                Console.WriteLine("\n=== 2. Assigning SMS trunk group to DID (clears the SMS trunk) ===");
                var assignGroup = Did.Build(did.Id!);
                assignGroup.SmsTrunkGroup = SmsTrunkGroup.Build(group.Id!);
                await client.Dids().UpdateAsync(assignGroup);
                await PrintSmsRoutingAsync(client, did.Id!);
            }
        }
        finally
        {
            // 3. Restore the original routing. Setting both to null on a built
            // resource unroutes the DID's inbound SMS.
            Console.WriteLine("\n=== 3. Restoring the DID's original SMS routing ===");
            var restore = Did.Build(did.Id!);
            if (originalTrunkId != null)
                restore.SmsTrunk = SmsTrunk.Build(originalTrunkId);
            else if (originalGroupId != null)
                restore.SmsTrunkGroup = SmsTrunkGroup.Build(originalGroupId);
            else
            {
                restore.SmsTrunk = null;
                restore.SmsTrunkGroup = null;
            }
            await client.Dids().UpdateAsync(restore);
            await PrintSmsRoutingAsync(client, did.Id!);
        }
    }

    private static async Task<Did?> FindDidAsync(DidwwClient client)
    {
        Console.WriteLine("=== Finding DID ===");
        var queryParams = new QueryParams()
            .Include("sms_trunk", "sms_trunk_group")
            .Page(1, 1);
        var dids = (await client.Dids().ListAsync(queryParams)).Data;
        if (dids.Count == 0)
        {
            Console.WriteLine("No DIDs found. Please order a DID first.");
            return null;
        }
        var did = dids[0];
        Console.WriteLine($"Using DID: {did.Number} ({did.Id})");
        return did;
    }

    private static async Task<SmsTrunk?> FindInboundTrunkAsync(DidwwClient client)
    {
        Console.WriteLine("\n=== Finding inbound SMS trunk ===");
        var trunks = (await client.SmsTrunks().ListAsync()).Data;
        var trunk = trunks.FirstOrDefault(t => t.AllowIn == true);
        if (trunk == null)
        {
            Console.WriteLine("No inbound SMS trunk found. Run the sms-trunks example or create one first.");
            return null;
        }
        Console.WriteLine($"Selected SMS trunk: {trunk.Name} ({trunk.Id})");
        return trunk;
    }

    // Returns the first SMS trunk group, or null when there is none.
    private static async Task<SmsTrunkGroup?> FindTrunkGroupAsync(DidwwClient client)
    {
        Console.WriteLine("\n=== Finding SMS trunk group ===");
        var groups = (await client.SmsTrunkGroups().ListAsync(new QueryParams().Page(1, 1))).Data;
        if (groups.Count == 0)
        {
            Console.WriteLine("No SMS trunk group found; step 2 is skipped.");
            return null;
        }
        var group = groups[0];
        Console.WriteLine($"Selected SMS trunk group: {group.Name} ({group.Id})");
        return group;
    }

    private static async Task PrintSmsRoutingAsync(DidwwClient client, string didId)
    {
        var queryParams = new QueryParams().Include("sms_trunk", "sms_trunk_group");
        var result = (await client.Dids().FindAsync(didId, queryParams)).Data;
        Console.WriteLine($"   sms_trunk       = {result.SmsTrunk?.Id ?? "null"}");
        Console.WriteLine($"   sms_trunk_group = {result.SmsTrunkGroup?.Id ?? "null"}");
    }

    private static async Task PrintRoutedDidsAsync(DidwwClient client, SmsTrunk trunk)
    {
        Console.WriteLine($"\n=== DIDs routed to {trunk.Name} ===");
        var routed = (await client.Dids().ListAsync(new QueryParams().Filter("sms_trunk.id", trunk.Id!))).Data;
        foreach (var d in routed)
            Console.WriteLine($"   {d.Number}");
    }
}
