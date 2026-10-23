using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// Lists Sender ID verifications (2026-10-22) with their SMS trunks, routes an
// active verification's outbound messages through an SMS trunk that sends
// them, and restores the verification's original trunk.
//
// A verification is created and reviewed outside the API; only its name and
// its SMS trunk can be changed, and a closed one cannot be changed at all.
// Only http_out, smpp_esme and smpp_smsc trunks are accepted.
public static class SenderIdVerificationsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("=== Sender ID Verifications ===");
        var verification = await ListVerificationsAsync(client);
        if (verification == null)
        {
            Console.WriteLine("No active Sender ID verification");
            return;
        }

        var trunk = await FindSendingTrunkAsync(client);
        if (trunk == null)
        {
            Console.WriteLine("No SMS trunk that sends messages");
            return;
        }

        Console.WriteLine($"\n=== Routing {verification.Reference} through {trunk.Name} ===");
        var route = SenderIdVerification.Build(verification.Id!);
        route.SmsTrunk = SmsTrunk.Build(trunk.Id!);
        var routed = (await client.SenderIdVerifications().UpdateAsync(route)).Data;
        try
        {
            Console.WriteLine($"Routed {routed.Reference} through {trunk.Id}");
            await PrintRoutedThroughAsync(client, trunk.Id!);
        }
        finally
        {
            // Restore the original trunk even when a later step fails.
            await RestoreTrunkAsync(client, verification);
        }
    }

    // Prints the verifications and returns the first active one.
    private static async Task<SenderIdVerification?> ListVerificationsAsync(DidwwClient client)
    {
        var queryParams = new QueryParams().Include("sms_trunk");
        var verifications = (await client.SenderIdVerifications().ListAsync(queryParams)).Data;
        SenderIdVerification? active = null;
        foreach (var v in verifications)
        {
            PrintVerification(v);
            if (active == null && v.IsActive)
                active = v;
        }
        return active;
    }

    private static void PrintVerification(SenderIdVerification v)
    {
        var routedThrough = v.SmsTrunk?.Name ?? "no SMS trunk";
        var senderIds = string.Join(", ", v.SenderIds ?? new List<string>());
        Console.WriteLine($"{v.Reference} {v.Name} ({v.SenderType}, {v.Status}): {senderIds} via {routedThrough}");
        if (v.IsChangesRequired)
        {
            var reasons = string.Join("; ", v.RejectReasons ?? new List<string>());
            Console.WriteLine($"  changes required: {reasons} {v.RejectComment}");
        }
    }

    // Returns the first SMS trunk that sends messages.
    private static async Task<SmsTrunk?> FindSendingTrunkAsync(DidwwClient client)
    {
        var trunks = (await client.SmsTrunks().ListAsync()).Data;
        return trunks.FirstOrDefault(t => t.AllowOut == true);
    }

    private static async Task PrintRoutedThroughAsync(DidwwClient client, string trunkId)
    {
        Console.WriteLine("\nActive verifications routed through this trunk:");
        var filtered = new QueryParams()
            .Filter("status", EnumWire.Value(SenderIdVerificationStatus.Active))
            .Filter("sms_trunk.id", trunkId);
        var active = (await client.SenderIdVerifications().ListAsync(filtered)).Data;
        foreach (var v in active)
            Console.WriteLine($"  {v.Reference} {v.Name}");
    }

    // Restores the original trunk. Setting SmsTrunk to null unassigns the
    // trunk, which stops outbound messages until a trunk is assigned again.
    private static async Task RestoreTrunkAsync(DidwwClient client, SenderIdVerification verification)
    {
        Console.WriteLine($"\n=== Restoring the original SMS trunk of {verification.Reference} ===");
        var original = verification.SmsTrunk;
        var restore = SenderIdVerification.Build(verification.Id!);
        restore.SmsTrunk = original == null ? null : SmsTrunk.Build(original.Id!);
        var restored = (await client.SenderIdVerifications().UpdateAsync(restore)).Data;
        Console.WriteLine(original != null
            ? $"{restored.Reference} is routed through {original.Name} again"
            : $"{restored.Reference} has no SMS trunk again");
    }
}
