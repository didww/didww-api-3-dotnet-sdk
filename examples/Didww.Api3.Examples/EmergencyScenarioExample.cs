using Didww.Api3.Http;
using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

// End-to-end Emergency Calling Service purchase flow (2026-04-16).
//
// Steps:
//   0. Find an address -> find available DID with emergency feature in that country -> order it -> wait.
//   1. Find the newly ordered DID with emergency feature that is not yet emergency-enabled.
//   2. Get emergency requirements for that DID's country + number_type.
//   3. Find an existing identity on the account.
//   4. Find an existing address on the account.
//   5. Validate the (requirement, address, identity) triple.
//   6. Create an emergency verification with the address and DID.
//   7. Fetch the verification to confirm status.
//   8. Fetch the auto-created emergency_calling_service via the verification.
public static class EmergencyScenarioExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("=== Emergency Scenario: End-to-End Flow ===\n");

        // === Step 0: Order a specific available DID with emergency feature ===
        Console.WriteLine("--- Step 0: Order an available DID with emergency feature ---");

        // Find an address first so we know what country to order in
        var addressesForOrder = await client.Addresses().ListAsync(
            new QueryParams().Include("country"));
        if (addressesForOrder.Data.Count == 0)
        {
            Console.WriteLine("No addresses on this account. Please create an address first.");
            return;
        }

        var addressForOrder = addressesForOrder.Data[0];
        var addressCountry = addressForOrder.Country;
        Console.WriteLine($"  Using address country: {addressCountry?.Name} ({addressCountry?.Id})");

        // Find an available DID with emergency feature in that country
        var availableDidsParams = new QueryParams()
            .Filter("did_group.features", "emergency")
            .Filter("country.id", addressCountry!.Id!)
            .Include("did_group", "did_group.stock_keeping_units")
            .Page(1, 1);
        var availableDidsResponse = await client.AvailableDids().ListAsync(availableDidsParams);

        if (availableDidsResponse.Data.Count == 0)
        {
            Console.WriteLine("No available DIDs with emergency feature in this country.");
            return;
        }

        var availableDid = availableDidsResponse.Data[0];
        var availableDidGroup = availableDid.DidGroup;
        var sku = availableDidGroup?.StockKeepingUnits?.FirstOrDefault();
        if (sku == null)
        {
            Console.WriteLine("No SKU found for this DID group.");
            return;
        }

        Console.WriteLine($"  Available DID: {availableDid.Number}");
        Console.WriteLine($"  DID Group: {availableDidGroup?.AreaName}");

        var orderItem = new AvailableDidOrderItem
        {
            AvailableDidId = availableDid.Id,
            SkuId = sku.Id
        };

        var order = new Order
        {
            Items = new List<OrderItemBase> { orderItem }
        };

        var orderResponse = await client.Orders().CreateAsync(order);
        var createdOrder = orderResponse.Data;
        Console.WriteLine($"  Order: {createdOrder.Id} -- {createdOrder.Status}");

        // Wait for order to complete
        for (var i = 0; i < 10; i++)
        {
            if (createdOrder.IsCompleted)
                break;
            await Task.Delay(5000);
            var refreshed = await client.Orders().FindAsync(createdOrder.Id!);
            createdOrder = refreshed.Data;
        }

        if (!createdOrder.IsCompleted)
        {
            Console.WriteLine($"  Order did not complete (status: {createdOrder.Status}).");
            return;
        }
        Console.WriteLine("  Order completed");

        // === Step 1: Find the newly ordered DID ===
        Console.WriteLine("\n--- Step 1: Find the newly ordered DID ---");
        var didParams = new QueryParams()
            .Filter("did_group.features", "emergency")
            .Filter("emergency_enabled", "false")
            .Include("did_group", "did_group.country", "emergency_calling_service")
            .Sort("-created_at")
            .Page(1, 10);
        var didResponse = await client.Dids().ListAsync(didParams);

        // Pick a DID that is not yet assigned to an ECS
        var did = didResponse.Data.FirstOrDefault(d => d.EmergencyCallingService == null);
        if (did == null)
        {
            Console.WriteLine("No available DID without an existing Emergency Calling Service.");
            return;
        }

        var didGroup = did.DidGroup;
        var country = didGroup?.Country;
        var numberType = didGroup?.NumberType;

        Console.WriteLine($"  DID:            {did.Number} ({did.Id})");
        Console.WriteLine($"  DID Group:      {didGroup?.Id}");
        Console.WriteLine($"  Country:        {country?.Name} ({country?.Id})");
        Console.WriteLine($"  Number Type:    {numberType}");

        // === Step 2: Get emergency requirements ===
        Console.WriteLine("\n--- Step 2: Get emergency requirements for country + number_type ---");
        var reqParams = new QueryParams();
        if (country != null)
            reqParams.Filter("country.id", country.Id!);
        if (numberType != null)
            reqParams.Filter("number_type", EnumWire.Value(numberType.Value));

        var reqResponse = await client.EmergencyRequirements().ListAsync(reqParams);
        var requirement = reqResponse.Data.FirstOrDefault();

        if (requirement == null)
        {
            Console.WriteLine($"No emergency requirements found for country {country?.Name} / {numberType}.");
            return;
        }

        Console.WriteLine($"  Emergency Requirement: {requirement.Id}");
        Console.WriteLine($"  Identity type:         {requirement.IdentityType}");
        Console.WriteLine($"  Address area level:    {requirement.AddressAreaLevel}");
        Console.WriteLine($"  Estimated setup time:  {requirement.EstimateSetupTime}");

        // === Step 3: Find an existing identity ===
        Console.WriteLine("\n--- Step 3: Find an existing identity ---");
        var identitiesResponse = await client.Identities().ListAsync();
        var identity = identitiesResponse.Data.FirstOrDefault();

        if (identity == null)
        {
            Console.WriteLine("No identities found on this account. Please create an identity first.");
            return;
        }
        Console.WriteLine($"  Identity: {identity.Id}");
        Console.WriteLine($"  Type:     {identity.IdentityType}");

        // === Step 4: Find an existing address ===
        Console.WriteLine("\n--- Step 4: Find an existing address ---");
        var addressesResponse = await client.Addresses().ListAsync();
        var address = addressesResponse.Data.FirstOrDefault();

        if (address == null)
        {
            Console.WriteLine("No addresses found on this account. Please create an address first.");
            return;
        }
        Console.WriteLine($"  Address: {address.Id}");

        // === Step 5: Validate the (requirement, address, identity) triple ===
        Console.WriteLine("\n--- Step 5: Validate emergency requirement ---");
        var validation = new EmergencyRequirementValidation
        {
            EmergencyRequirement = EmergencyRequirement.Build(requirement.Id!),
            Address = Address.Build(address.Id!),
            Identity = Identity.Build(identity.Id!)
        };

        try
        {
            await client.EmergencyRequirementValidations().CreateAsync(validation);
            Console.WriteLine("  Validation passed -- this combination can be used for emergency calling.");
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"  Validation failed: {ex.Message}");
            Console.WriteLine("  Cannot proceed without a valid requirement/address/identity combination.");
            return;
        }

        // === Step 6: Create an emergency verification ===
        Console.WriteLine("\n--- Step 6: Create emergency verification ---");
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var verification = new EmergencyVerification
        {
            CallbackUrl = "https://example.com/webhooks/emergency",
            CallbackMethod = CallbackMethod.Post,
            ExternalReferenceId = $"dotnet-scenario-{suffix}",
            Address = Address.Build(address.Id!),
            Dids = new List<Did> { Did.Build(did.Id!) }
        };

        try
        {
            var verificationResponse = await client.EmergencyVerifications().CreateAsync(verification);
            var created = verificationResponse.Data;
            Console.WriteLine($"  Created verification: {created.Id}");
            Console.WriteLine($"  Reference:            {created.Reference}");
            Console.WriteLine($"  Status:               {created.Status}");
            Console.WriteLine($"  External Reference:   {created.ExternalReferenceId}");

            // === Step 7: Fetch the verification to confirm status ===
            Console.WriteLine("\n--- Step 7: Fetch the created verification ---");
            var fetchParams = new QueryParams()
                .Include("address", "emergency_calling_service", "dids");
            var fetched = await client.EmergencyVerifications()
                .FindAsync(created.Id!, fetchParams);
            var fetchedData = fetched.Data;
            Console.WriteLine($"  Verification: {fetchedData.Id}");
            Console.WriteLine($"  Status:       {fetchedData.Status}");
            if (fetchedData.Dids != null && fetchedData.Dids.Count > 0)
                Console.WriteLine($"  DIDs:         {string.Join(", ", fetchedData.Dids.Select(d => d.Number))}");
            Console.WriteLine($"  Address:      {fetchedData.Address?.Id}");

            // === Step 8: Fetch the auto-created emergency_calling_service ===
            Console.WriteLine("\n--- Step 8: Fetch emergency calling service ---");
            var ecs = fetchedData.EmergencyCallingService;
            if (ecs != null)
            {
                var ecsParams = new QueryParams()
                    .Include("country", "dids");
                var service = await client.EmergencyCallingServices()
                    .FindAsync(ecs.Id!, ecsParams);
                var svc = service.Data;
                Console.WriteLine($"  Service:        {svc.Id}");
                Console.WriteLine($"  Name:           {svc.Name}");
                Console.WriteLine($"  Reference:      {svc.Reference}");
                Console.WriteLine($"  Status:         {svc.Status}");
                Console.WriteLine($"  Country:        {svc.Country?.Name}");
                Console.WriteLine($"  Number Type:    {svc.NumberType}");
                if (svc.Dids != null && svc.Dids.Count > 0)
                    Console.WriteLine($"  Attached DIDs:  {string.Join(", ", svc.Dids.Select(d => d.Number))}");
            }
            else
            {
                Console.WriteLine("  No emergency_calling_service linked yet (may be created asynchronously).");
            }
        }
        catch (System.Exception ex)
        {
            Console.WriteLine($"  Error creating verification: {ex.Message}");
            Console.WriteLine("  Failed to create emergency verification.");
            return;
        }

        Console.WriteLine("\n=== Emergency Scenario Complete ===");
    }
}
