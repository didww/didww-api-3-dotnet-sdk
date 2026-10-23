using Didww.Api3.Http;

namespace Didww.Api3.Examples;

// Lists emergency service requirements for a country/number_type (2026-04-16).
//
// Emergency requirements describe what address precision, identity type,
// and supporting fields an end-customer must provide to enable 911/112
// on a DID. Each record reports a price preview in meta when the
// customer has a matching emergency plan.
public static class EmergencyRequirementsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("=== Emergency Requirements ===");
        var queryParams = new QueryParams().Include("country");
        var response = await client.EmergencyRequirements().ListAsync(queryParams);
        var requirements = response.Data;
        Console.WriteLine($"Found {requirements.Count} emergency requirements");

        foreach (var req in requirements.Take(5))
        {
            Console.WriteLine($"\nRequirement: {req.Id}");
            Console.WriteLine($"  Country: {req.Country?.Name}");
            Console.WriteLine($"  Number Type: {req.NumberType}");
            Console.WriteLine($"  Identity type required: {req.IdentityType}");
            Console.WriteLine($"  Address area level: {req.AddressAreaLevel}");
            if (req.AddressMandatoryFields != null)
                Console.WriteLine($"  Address mandatory fields: {string.Join(", ", req.AddressMandatoryFields)}");
            Console.WriteLine($"  Estimated setup time: {req.EstimateSetupTime}");
            if (req.RequirementRestrictionMessage != null)
                Console.WriteLine($"  Restriction: {req.RequirementRestrictionMessage}");
            if (req.Meta != null)
            {
                Console.WriteLine($"  Setup price: {req.Meta.GetValueOrDefault("setup_price")}");
                Console.WriteLine($"  Monthly price: {req.Meta.GetValueOrDefault("monthly_price")}");
            }
        }

        // Filter by country
        if (requirements.Count > 0 && requirements[0].Country != null)
        {
            var countryId = requirements[0].Country!.Id;
            Console.WriteLine($"\n=== Requirements for country {requirements[0].Country!.Name} ===");
            var perCountryParams = new QueryParams()
                .Filter("country.id", countryId!);
            var perCountry = await client.EmergencyRequirements().ListAsync(perCountryParams);
            Console.WriteLine($"Found {perCountry.Data.Count} requirements");
        }
    }
}
