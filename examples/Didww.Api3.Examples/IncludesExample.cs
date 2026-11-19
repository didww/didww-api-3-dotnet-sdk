using Didww.Api3.Http;

namespace Didww.Api3.Examples;

// Reads resources with their related resources included (?include=...).
//
// An included relationship is a full object: Region.Country.Name,
// DidGroup.StockKeepingUnits[i].MonthlyPrice and so on. Without the include
// the API sends only a link for the relationship, and the SDK leaves the
// property null. Includes can be nested with a dot: did_group.stock_keeping_units.
public static class IncludesExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("=== Regions without include: country is null ===");
        var plainRegions = (await client.Regions().ListAsync(new QueryParams().Page(1, 3))).Data;
        foreach (var region in plainRegions)
            Console.WriteLine($"  {region.Name} ({region.Iso}) - country: {region.Country?.Name ?? "null (not included)"}");

        Console.WriteLine("\n=== Regions with country ===");
        var regions = (await client.Regions().ListAsync(new QueryParams()
            .Include("country")
            .Page(1, 3))).Data;
        foreach (var region in regions)
            Console.WriteLine($"  {region.Name} ({region.Iso}) - country: {region.Country?.Name}");

        Console.WriteLine("\n=== DID groups with SKUs ===");
        var groups = (await client.DidGroups().ListAsync(new QueryParams()
            .Include("stock_keeping_units")
            .Page(1, 3))).Data;
        foreach (var group in groups)
        {
            var skus = group.StockKeepingUnits ?? [];
            Console.WriteLine($"  {group.Prefix} {group.AreaName} - {skus.Count} SKU(s)");
            foreach (var sku in skus)
                Console.WriteLine($"    {sku.MonthlyPrice}/mo, {sku.ChannelsIncludedCount} channels");
        }

        Console.WriteLine("\n=== Available DIDs with did_group.stock_keeping_units ===");
        var available = (await client.AvailableDids().ListAsync(new QueryParams()
            .Include("did_group.stock_keeping_units")
            .Page(1, 3))).Data;
        foreach (var ad in available)
        {
            var group = ad.DidGroup;
            Console.WriteLine($"  {ad.Number} - group: {group?.Prefix} {group?.AreaName}, {group?.StockKeepingUnits?.Count ?? 0} SKU(s)");
        }

        Console.WriteLine("\n=== DIDs with voice_in_trunk ===");
        var dids = (await client.Dids().ListAsync(new QueryParams()
            .Include("voice_in_trunk")
            .Page(1, 3))).Data;
        foreach (var did in dids)
            Console.WriteLine($"  {did.Number} - trunk: {did.VoiceInTrunk?.Name ?? "(none)"}");
    }
}
