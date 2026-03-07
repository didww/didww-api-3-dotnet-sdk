using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

public static class IdentityAddressProofsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Create Identity ---");

        // Get a country for the identity
        var countries = await client.Countries().ListAsync(new QueryParams().Page(1, 1));
        if (countries.Data.Count == 0)
        {
            Console.WriteLine("  No countries found, skipping.");
            return;
        }

        var country = countries.Data[0];
        Console.WriteLine($"  Using country: {country.Name} ({country.Iso})");

        var identity = new Identity
        {
            FirstName = "Jane",
            LastName = "Smith",
            PhoneNumber = "5551234567",
            IdentityType = IdentityType.Personal,
            Country = Country.Build(country.Id!)
        };

        var identityResponse = await client.Identities().CreateAsync(identity);
        var createdIdentity = identityResponse.Data;
        Console.WriteLine($"  Identity created: {createdIdentity.Id}");
        Console.WriteLine($"    Name: {createdIdentity.FirstName} {createdIdentity.LastName}");
        Console.WriteLine($"    Type: {createdIdentity.IdentityType}");

        Console.WriteLine("\n--- Create Address ---");
        var address = new Address
        {
            CityName = "New York",
            PostalCode = "10001",
            AddressLine = "123 Main Street",
            Description = "SDK example address",
            Identity = Identity.Build(createdIdentity.Id!),
            Country = Country.Build(country.Id!)
        };

        var addressResponse = await client.Addresses().CreateAsync(address);
        var createdAddress = addressResponse.Data;
        Console.WriteLine($"  Address created: {createdAddress.Id}");
        Console.WriteLine($"    City: {createdAddress.CityName}");
        Console.WriteLine($"    Address: {createdAddress.AddressLine}");

        Console.WriteLine("\n--- List Proof Types ---");
        var proofTypes = await client.ProofTypes().ListAsync();
        foreach (var pt in proofTypes.Data.Take(5))
        {
            Console.WriteLine($"  {pt.Name} (entity: {pt.EntityType})");
        }

        Console.WriteLine("\n--- Cleanup ---");
        await client.Addresses().DeleteAsync(createdAddress.Id!);
        Console.WriteLine($"  Address deleted: {createdAddress.Id}");

        await client.Identities().DeleteAsync(createdIdentity.Id!);
        Console.WriteLine($"  Identity deleted: {createdIdentity.Id}");
    }
}
