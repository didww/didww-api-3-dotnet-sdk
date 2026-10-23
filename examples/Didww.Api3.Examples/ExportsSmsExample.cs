using System.IO.Compression;
using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// Creates SMS exports (sms_in / sms_out, 2026-10-22) for yesterday and reads
// the columns of a completed SMS-Out export.
//
// Filters on SMS exports (Export.Filters):
//   * "from", "to" (required): "YYYY-MM-DD HH:MM:SS" UTC; from inclusive, to exclusive
//   * "source_address_eq" / "source_address_cont", "destination_address_eq" /
//     "destination_address_cont", "status", "sms_id"
//   * "sms_trunk.id": one id or a list of ids
//   * sms_in only: "trunk_type", one or more of the ExportTrunkTypes values
//   * sms_out only: "service_type" ("a2p" or "p2p"), "sender_id_verification.id"
public static class ExportsSmsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = $"{today.AddDays(-1):yyyy-MM-dd} 00:00:00"; // inclusive
        var to = $"{today:yyyy-MM-dd} 00:00:00";               // exclusive
        var suffix = Guid.NewGuid().ToString("N")[..8];

        Console.WriteLine("=== Creating SMS-Out Export (A2P, yesterday) ===");
        try
        {
            var smsOut = (await client.Exports().CreateAsync(new Export
            {
                ExportType = ExportType.SmsOut,
                Filters = new Dictionary<string, object>
                {
                    { "from", from },
                    { "to", to },
                    { "service_type", "a2p" }
                },
                ExternalReferenceId = "dotnet-sms-out-" + suffix
            })).Data;
            Console.WriteLine($"Created SMS-Out export: {smsOut.Id} (status: {smsOut.Status})");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error creating SMS-Out export: {ex.Message}");
        }

        Console.WriteLine("\n=== Creating SMS-In Export (HTTP IN and email trunks, yesterday) ===");
        try
        {
            var smsIn = (await client.Exports().CreateAsync(new Export
            {
                ExportType = ExportType.SmsIn,
                Filters = new Dictionary<string, object>
                {
                    { "from", from },
                    { "to", to },
                    { "trunk_type", new List<string> { ExportTrunkTypes.HttpIn, ExportTrunkTypes.Email } }
                },
                ExternalReferenceId = "dotnet-sms-in-" + suffix
            })).Data;
            Console.WriteLine($"Created SMS-In export: {smsIn.Id} (status: {smsIn.Status})");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"Error creating SMS-In export: {ex.Message}");
        }

        Console.WriteLine("\n=== Completed SMS-Out exports ===");
        var queryParams = new QueryParams()
            .Filter("export_type", EnumWire.Value(ExportType.SmsOut))
            .Filter("status", EnumWire.Value(ExportStatus.Completed));
        var completed = (await client.Exports().ListAsync(queryParams)).Data;
        Console.WriteLine($"Found {completed.Count} completed SMS-Out exports");
        if (completed.Count == 0 || completed[0].Url == null)
            return;

        // The export file is a gzip-compressed CSV; read its header line.
        var export = completed[0];
        using var compressed = new MemoryStream();
        await client.DownloadExportAsync(export.Url!, compressed);
        compressed.Position = 0;
        await using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        using var reader = new StreamReader(gzip);
        var header = await reader.ReadLineAsync();
        Console.WriteLine($"Export {export.Id} ({export.ExportType}) columns: {header}");
    }
}
