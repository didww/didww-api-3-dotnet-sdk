using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;

namespace Didww.Api3.Examples;

// Creates an identity and an address, encrypts and uploads a PDF file (one
// file per request, 2026-04-16), attaches a proof to the identity and one to
// the address, and prints their verified flags. The address, the identity and
// the uploaded files are deleted at the end, also when a step fails.
public static class IdentityAddressProofsExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        // Step 1: a country for the identity and the address
        var countries = (await client.Countries().ListAsync(new QueryParams().Page(1, 1))).Data;
        if (countries.Count == 0)
        {
            Console.WriteLine("No countries found, skipping.");
            return;
        }
        var country = countries[0];
        Console.WriteLine($"Using country: {country.Name} ({country.Iso})");

        // Step 2: create the identity (2026-04-16 adds birth_country)
        Console.WriteLine("\n--- Create Identity ---");
        var identity = (await client.Identities().CreateAsync(new Identity
        {
            FirstName = "John",
            LastName = "Doe",
            PhoneNumber = "12125551234",
            IdentityType = IdentityType.Personal,
            Country = Country.Build(country.Id!),
            BirthCountry = Country.Build(country.Id!)
        }, new QueryParams().Include("country", "birth_country"))).Data;
        Console.WriteLine($"  Identity created: {identity.Id} ({identity.FirstName} {identity.LastName})");
        Console.WriteLine($"    Country: {identity.Country?.Name}, birth country: {identity.BirthCountry?.Name}");

        string? addressId = null;
        var fileIds = new List<string>();
        try
        {
            // Step 3: create an address of the identity
            Console.WriteLine("\n--- Create Address ---");
            var address = (await client.Addresses().CreateAsync(new Address
            {
                CityName = "New York",
                PostalCode = "10001",
                AddressLine = "123 Main St",
                Description = "SDK example address",
                Identity = Identity.Build(identity.Id!),
                Country = Country.Build(country.Id!)
            })).Data;
            addressId = address.Id;
            Console.WriteLine($"  Address created: {address.Id} ({address.AddressLine}, {address.CityName})");

            // Step 4: proof types for a personal identity and for an address
            Console.WriteLine("\n--- Proof Types ---");
            var proofTypes = (await client.ProofTypes().ListAsync()).Data;
            var identityProofType = proofTypes.FirstOrDefault(pt => pt.EntityType == EnumWire.Value(IdentityType.Personal));
            var addressProofType = proofTypes.FirstOrDefault(pt => pt.EntityType == "address");
            Console.WriteLine($"  Identity proof type: {identityProofType?.Name ?? "none found"}");
            Console.WriteLine($"  Address proof type: {addressProofType?.Name ?? "none found"}");

            // Step 5: encrypt the file and upload one copy per proof
            Console.WriteLine("\n--- Upload Encrypted Files ---");
            var fileData = SamplePdf.Create("Proof document");
            var encrypt = new Encrypt(client);
            foreach (var label in new[] { "identity_proof", "address_proof" })
            {
                var fileId = await client.UploadEncryptedFileAsync(
                    encrypt.EncryptData(fileData), label + ".pdf.enc", encrypt.Fingerprint, label + ".pdf");
                fileIds.Add(fileId);
                Console.WriteLine($"  Uploaded {label}.pdf: {fileId}");
            }

            // Steps 6 and 7: a proof for the identity and one for the address
            Console.WriteLine("\n--- Create Proofs ---");
            if (identityProofType != null)
                await CreateProofAsync(client, Identity.Build(identity.Id!), identityProofType, fileIds[0], "identity");
            if (addressProofType != null)
                await CreateProofAsync(client, Address.Build(address.Id!), addressProofType, fileIds[1], "address");

            // Step 8: the verified flags, read again after the proofs
            Console.WriteLine("\n--- Verified Flags ---");
            var identityNow = (await client.Identities().FindAsync(identity.Id!)).Data;
            var addressNow = (await client.Addresses().FindAsync(address.Id!)).Data;
            Console.WriteLine($"  Identity {identityNow.Id}: verified={identityNow.Verified}");
            Console.WriteLine($"  Address {addressNow.Id}: verified={addressNow.Verified}");
        }
        finally
        {
            // Step 9: clean up, every step also when an earlier one fails
            Console.WriteLine("\n--- Cleanup ---");
            var steps = new List<Func<Task>>();
            if (addressId != null)
            {
                steps.Add(async () =>
                {
                    await client.Addresses().DeleteAsync(addressId);
                    Console.WriteLine($"  Address deleted: {addressId}");
                });
            }
            steps.Add(async () =>
            {
                await client.Identities().DeleteAsync(identity.Id!);
                Console.WriteLine($"  Identity deleted: {identity.Id}");
            });
            steps.AddRange(fileIds.Select(fileId => (Func<Task>)(async () =>
            {
                await client.EncryptedFiles().DeleteAsync(fileId);
                Console.WriteLine($"  Encrypted file deleted: {fileId}");
            })));
            await CleanupSteps.RunAllAsync(steps);
        }
    }

    private static async Task CreateProofAsync(DidwwClient client, BaseResource entity, ProofType proofType,
        string fileId, string label)
    {
        var proof = new Proof
        {
            Entity = entity,
            ProofType = ProofType.Build(proofType.Id!),
            Files = new List<EncryptedFile> { EncryptedFile.Build(fileId) }
        };
        var created = (await client.Proofs().CreateAsync(proof, new QueryParams().Include("proof_type"))).Data;
        Console.WriteLine($"  {label} proof created: {created.Id} (type={created.ProofType?.Name})");
    }
}
