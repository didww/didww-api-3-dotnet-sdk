using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// CRUD for SMS trunk groups (2026-10-22): creates two inbound SMS trunks, puts
// them in a group, lists groups, and deletes the group.
//
// A group spreads a DID's inbound messages over its trunks by their priority
// (higher first). http_out trunks cannot join a group. Deleting a group also
// deletes the trunks in it.
public static class SmsTrunkGroupsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        Console.WriteLine("=== Creating SMS Trunks ===");
        var primary = (await client.SmsTrunks().CreateAsync(new SmsTrunk
        {
            Name = "DotNet primary " + suffix,
            Priority = 200,
            Configuration = new HttpInConfiguration
            {
                Url = "https://example.com/sms",
                HttpMethod = SmsHttpMethod.Get,
                QueryParameters = new Dictionary<string, object>
                {
                    { "text", "{SMS_TEXT}" },
                    { "from", "{SMS_SRC_ADDR}" }
                }
            }
        })).Data;
        Console.WriteLine($"Created SMS trunk: {primary.Id} - {primary.Name}");

        SmsTrunk fallback;
        try
        {
            fallback = (await client.SmsTrunks().CreateAsync(new SmsTrunk
            {
                Name = "DotNet fallback " + suffix,
                Priority = 100,
                Configuration = new SmsToEmailConfiguration
                {
                    SendTo = "sms@example.com",
                    Subject = "Inbound SMS",
                    Message = "You have a new message."
                }
            })).Data;
        }
        catch
        {
            await client.SmsTrunks().DeleteAsync(primary.Id!);
            throw;
        }
        Console.WriteLine($"Created SMS trunk: {fallback.Id} - {fallback.Name}");

        Console.WriteLine("\n=== Creating SMS Trunk Group ===");
        SmsTrunkGroup group;
        try
        {
            group = (await client.SmsTrunkGroups().CreateAsync(new SmsTrunkGroup
            {
                Name = "DotNet SMS Group " + suffix,
                ExternalReferenceId = "dotnet-sms-group-" + suffix,
                SmsTrunks = new List<SmsTrunk> { SmsTrunk.Build(primary.Id!), SmsTrunk.Build(fallback.Id!) }
            })).Data;
        }
        catch
        {
            await client.SmsTrunks().DeleteAsync(primary.Id!);
            await client.SmsTrunks().DeleteAsync(fallback.Id!);
            throw;
        }
        Console.WriteLine($"Created SMS trunk group: {group.Id} - {group.Name} ({group.SmsTrunksCount} trunks)");

        try
        {
            Console.WriteLine("\n=== Existing SMS Trunk Groups ===");
            var queryParams = new QueryParams()
                .Include("sms_trunks")
                .Page(1, 5);
            var groups = (await client.SmsTrunkGroups().ListAsync(queryParams)).Data;
            foreach (var g in groups)
            {
                var names = string.Join(", ", (g.SmsTrunks ?? new List<SmsTrunk>()).Select(t => t.Name));
                Console.WriteLine($"{g.Name} ({g.SmsTrunksCount} trunks) {names}");
            }
        }
        finally
        {
            Console.WriteLine("\n=== Deleting SMS Trunk Group (and its trunks) ===");
            await client.SmsTrunkGroups().DeleteAsync(group.Id!);
            Console.WriteLine("SMS trunk group deleted");
        }
    }
}
