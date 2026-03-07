using Didww.Api3.Http;

namespace Didww.Api3.Examples;

public static class RegionsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- List Regions ---");

        // Get first country to filter by
        var countries = await client.Countries().ListAsync(new QueryParams().Page(1, 1));
        if (countries.Data.Count == 0)
        {
            Console.WriteLine("  No countries found, skipping.");
            return;
        }

        var countryId = countries.Data[0].Id!;
        var queryParams = new QueryParams()
            .Filter("country.id", countryId)
            .Include("country")
            .Sort("name")
            .Page(1, 5);

        var response = await client.Regions().ListAsync(queryParams);
        foreach (var region in response.Data)
        {
            Console.WriteLine($"  {region.Name} (Country: {region.Country?.Name})");
        }

        if (response.Data.Count > 0)
        {
            Console.WriteLine("\n--- Find Region ---");
            var qp = new QueryParams().Include("country");
            var found = await client.Regions().FindAsync(response.Data[0].Id!, qp);
            Console.WriteLine($"  Found: {found.Data.Name}");
        }
    }
}
