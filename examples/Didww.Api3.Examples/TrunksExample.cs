using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

public static class TrunksExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Create SIP Voice In Trunk ---");
        var sipConfig = new SipConfiguration
        {
            Username = "myuser",
            Host = "203.0.113.1",
            Port = 5060,
            // Codecs in priority order: the first has the highest priority.
            Codecs = new List<Codec> { Codec.Pcmu, Codec.Pcma, Codec.TelephoneEvent },
            IceMode = IceMode.Accept
        };

        var trunk = new VoiceInTrunk
        {
            Name = "My SIP Trunk",
            Configuration = sipConfig
        };

        var response = await client.VoiceInTrunks().CreateAsync(trunk);
        Console.WriteLine($"  Trunk created: {response.Data.Id} ({response.Data.Name})");

        Console.WriteLine("\n--- List Voice In Trunks ---");
        var listResponse = await client.VoiceInTrunks().ListAsync();
        foreach (var t in listResponse.Data)
        {
            Console.WriteLine($"  {t.Name} (ID: {t.Id})");
        }

        Console.WriteLine("\n--- Delete Voice In Trunk ---");
        await client.VoiceInTrunks().DeleteAsync(response.Data.Id!);
        Console.WriteLine($"  Trunk deleted: {response.Data.Id}");
    }
}
