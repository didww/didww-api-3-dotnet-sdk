using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// CRUD for SMS trunks (2026-10-22): lists trunks, creates an HTTP IN trunk,
// updates its configuration, regenerates the credentials of an HTTP OUT trunk,
// and deletes both trunks.
//
// Configuration types (Didww.Api3.Resource.Configuration.SmsTrunkConfiguration):
//   * SmsToEmailConfiguration, HttpInConfiguration: DIDWW delivers inbound
//     messages by email or HTTP
//   * HttpOutConfiguration, SmppEsmeConfiguration: you connect to DIDWW; DIDWW
//     generates the credentials, which are read-only
//   * SmppSmscConfiguration: DIDWW binds to your SMSC
//
// The read-only AllowIn / AllowOut tell which directions a trunk handles.
public static class SmsTrunksExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        await ListTrunksAsync(client);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        Console.WriteLine("\n=== Creating HTTP IN Trunk ===");
        var created = (await client.SmsTrunks().CreateAsync(new SmsTrunk
        {
            Name = "DotNet HTTP IN " + suffix,
            ExternalReferenceId = "dotnet-sms-" + suffix,
            Configuration = new HttpInConfiguration
            {
                Url = "https://example.com/sms",
                HttpMethod = SmsHttpMethod.Post,
                BodyType = SmsBodyType.Json,
                // Placeholders: {SMS_TEXT}, {SMS_TEXT_BASE64_ENCODED}, {SMS_SRC_ADDR}, {SMS_DST_ADDR}, {SMS_TIME}
                Body = "{\"text\":\"{SMS_TEXT}\",\"from\":\"{SMS_SRC_ADDR}\",\"to\":\"{SMS_DST_ADDR}\"}",
                ValidateHttpsCertificate = true
            }
        })).Data;
        Console.WriteLine($"Created SMS trunk: {created.Id} - {created.Name}");
        if (created.Configuration is HttpInConfiguration createdConfig)
            Console.WriteLine($"  Configuration: {createdConfig.ConfigurationType} -> {createdConfig.Url}");

        try
        {
            // The configuration type cannot change; attributes left out keep their values.
            Console.WriteLine("\n=== Updating HTTP IN Trunk ===");
            var update = SmsTrunk.Build(created.Id!);
            update.Name = "DotNet HTTP IN " + suffix + " (v2)";
            update.Configuration = new HttpInConfiguration { Url = "https://example.com/sms-v2" };
            try
            {
                var updated = (await client.SmsTrunks().UpdateAsync(update)).Data;
                if (updated.Configuration is HttpInConfiguration updatedConfig)
                    Console.WriteLine($"Updated: {updated.Name} -> {updatedConfig.Url}");
            }
            catch (System.Exception ex)
            {
                Console.WriteLine($"Error updating SMS trunk: {ex.Message}");
            }

            await HttpOutRoundTripAsync(client, suffix);
        }
        finally
        {
            Console.WriteLine("\n=== Deleting HTTP IN Trunk ===");
            await client.SmsTrunks().DeleteAsync(created.Id!);
            Console.WriteLine("SMS trunk deleted");
        }
    }

    private static async Task ListTrunksAsync(DidwwClient client)
    {
        Console.WriteLine("=== Existing SMS Trunks ===");
        var queryParams = new QueryParams()
            .Include("sms_trunk_group")
            .Page(1, 10);
        var trunks = (await client.SmsTrunks().ListAsync(queryParams)).Data;
        Console.WriteLine($"Found {trunks.Count} SMS trunks");
        foreach (var trunk in trunks)
            PrintTrunk(trunk);
    }

    private static void PrintTrunk(SmsTrunk trunk)
    {
        Console.WriteLine($"{trunk.Name} [{trunk.Configuration?.ConfigurationType ?? "none"}]");
        Console.WriteLine($"  ID: {trunk.Id}");
        Console.WriteLine($"  Inbound: {trunk.AllowIn}, outbound: {trunk.AllowOut}");
        if (trunk.SmsTrunkGroup != null)
            Console.WriteLine($"  Group: {trunk.SmsTrunkGroup.Name}");
        if (trunk.Host != null)
            Console.WriteLine($"  Submit URL: {trunk.Host}");
    }

    // Creates an HTTP OUT trunk, regenerates its credentials and deletes it.
    // The generated credentials of http_out and smpp_esme trunks are rotated
    // with an SmsTrunkRegenerateCredential and read back from the trunk.
    private static async Task HttpOutRoundTripAsync(DidwwClient client, string suffix)
    {
        Console.WriteLine("\n=== Creating HTTP OUT Trunk and Regenerating Its Credentials ===");
        SmsTrunk httpOut;
        try
        {
            httpOut = (await client.SmsTrunks().CreateAsync(new SmsTrunk
            {
                Name = "DotNet HTTP OUT " + suffix,
                Configuration = new HttpOutConfiguration
                {
                    CallbackUrl = "https://example.com/sms/status",
                    AllowedIps = new List<string> { "203.0.113.10" }
                }
            })).Data;
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error creating HTTP OUT trunk: {ex.Message}");
            return;
        }
        Console.WriteLine($"Created SMS trunk: {httpOut.Id} - {httpOut.Name}");

        try
        {
            await RegenerateCredentialsAsync(client, httpOut.Id!);
        }
        finally
        {
            await client.SmsTrunks().DeleteAsync(httpOut.Id!);
            Console.WriteLine("HTTP OUT trunk deleted");
        }
    }

    private static async Task RegenerateCredentialsAsync(DidwwClient client, string trunkId)
    {
        try
        {
            await client.SmsTrunkRegenerateCredentials().CreateAsync(
                new SmsTrunkRegenerateCredential { SmsTrunk = SmsTrunk.Build(trunkId) });
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error regenerating credentials: {ex.Message}");
            return;
        }

        // The response carries no credentials: read the trunk again.
        var trunk = (await client.SmsTrunks().FindAsync(trunkId)).Data;
        if (trunk.Configuration is HttpOutConfiguration config)
        {
            // Use config.AuthName and config.AuthPassword to submit messages to
            // trunk.Host. Printing config itself masks them.
            Console.WriteLine($"Credentials regenerated: {config}");
            Console.WriteLine($"  Submit URL: {trunk.Host}");
        }
    }
}
