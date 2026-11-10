using Didww.Api3.Resource;
using Didww.Api3.Resource.Configuration.AuthenticationMethod;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// CRUD for voice out trunks using 2026-04-16 polymorphic authentication_method.
// Note: Voice Out Trunks require additional account configuration.
// Contact DIDWW support to enable.
public static class VoiceOutTrunksExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- List Voice Out Trunks ---");
        var listResponse = await client.VoiceOutTrunks().ListAsync();
        Console.WriteLine($"  Found {listResponse.Data.Count} voice out trunks");
        foreach (var t in listResponse.Data.Take(5))
        {
            Console.WriteLine($"  {t.Name} ({t.Status})");
            Console.WriteLine($"    ID: {t.Id}");
            Console.WriteLine($"    Auth type: {t.AuthenticationMethod?.GetType().Name}");
            switch (t.AuthenticationMethod)
            {
                case CredentialsAndIpAuthenticationMethod cai:
                    Console.WriteLine($"    Username: {cai.Username}");
                    break;
                case IpOnlyAuthenticationMethod ip:
                    Console.WriteLine($"    Allowed SIP IPs: {string.Join(", ", ip.AllowedSipIps ?? [])}");
                    break;
            }
            Console.WriteLine($"    Default DST Action: {t.DefaultDstAction}");
            Console.WriteLine($"    On CLI Mismatch: {t.OnCliMismatchAction}");
            Console.WriteLine($"    External Reference ID: {t.ExternalReferenceId}");
            Console.WriteLine($"    Emergency Enable All: {t.EmergencyEnableAll}");
            Console.WriteLine($"    RTP Timeout: {t.RtpTimeout}");
            Console.WriteLine($"    Codecs: {string.Join(", ", t.Codecs ?? [])}");
            Console.WriteLine($"    ICE / RTCP-mux / RTCP feedback: {t.IceMode} / {t.RtcpMuxMode} / {t.RtcpFeedbackMode}");
            Console.WriteLine();
        }

        // Create a voice out trunk with credentials_and_ip authentication
        Console.WriteLine("\n--- Creating Voice Out Trunk (credentials_and_ip) ---");
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var trunk = new VoiceOutTrunk
        {
            Name = "Outbound Trunk " + suffix,
            AuthenticationMethod = new CredentialsAndIpAuthenticationMethod
            {
                // RFC 5737 TEST-NET-3 range — replace with real infrastructure CIDR
                AllowedSipIps = new List<string> { "203.0.113.0/24" },
                TechPrefix = ""
            },
            DefaultDstAction = DefaultDstAction.AllowAll,
            OnCliMismatchAction = OnCliMismatchAction.RejectCall,
            ExternalReferenceId = "dotnet-example-" + suffix,
            RtpTimeout = 60,
            // Codecs in priority order: the first has the highest priority.
            Codecs = new List<VoiceOutCodec> { VoiceOutCodec.Opus, VoiceOutCodec.Pcma, VoiceOutCodec.TelephoneEvent },
            IceMode = IceMode.Accept
        };

        var response = await client.VoiceOutTrunks().CreateAsync(trunk);
        var created = response.Data;
        Console.WriteLine($"  Created: {created.Id}");
        Console.WriteLine($"    Name: {created.Name}");
        Console.WriteLine($"    Auth type: {created.AuthenticationMethod?.GetType().Name}");
        if (created.AuthenticationMethod is CredentialsAndIpAuthenticationMethod createdCai)
        {
            Console.WriteLine($"    Username: {createdCai.Username}");
            Console.WriteLine($"    Password: {createdCai.Password}");
        }
        Console.WriteLine($"    Status: {created.Status}");
        Console.WriteLine($"    Codecs: {string.Join(", ", created.Codecs ?? [])}");
        Console.WriteLine($"    External Reference: {created.ExternalReferenceId}");

        // Update trunk - change allowed IPs and tech prefix
        Console.WriteLine("\n--- Updating Voice Out Trunk ---");
        created.Name = "Updated Outbound Trunk " + suffix;
        created.AuthenticationMethod = new CredentialsAndIpAuthenticationMethod
        {
            AllowedSipIps = new List<string> { "203.0.113.0/24" },
            TechPrefix = "9"
        };

        var updated = (await client.VoiceOutTrunks().UpdateAsync(created)).Data;
        Console.WriteLine($"  Updated name: {updated.Name}");
        Console.WriteLine($"    Auth type: {updated.AuthenticationMethod?.GetType().Name}");
        if (updated.AuthenticationMethod is CredentialsAndIpAuthenticationMethod updatedCai)
        {
            Console.WriteLine($"    Username: {updatedCai.Username}");
        }

        Console.WriteLine("\n--- Delete Voice Out Trunk ---");
        await client.VoiceOutTrunks().DeleteAsync(created.Id!);
        Console.WriteLine($"  Deleted: {created.Id}");
    }
}
