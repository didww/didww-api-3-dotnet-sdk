using Didww.Api3.Resource;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class AddressTest : BaseTest
{
    [Fact]
    public async Task TestListAddresses()
    {
        StubGet("addresses", "addresses/index.json");

        var response = await Client.Addresses().ListAsync();
        var addresses = response.Data;

        addresses.Should().NotBeEmpty();

        var first = addresses[0];
        first.Id.Should().Be("9d3f2582-a292-4b6b-828b-74c78b5a3780");
        first.CityName.Should().Be("Odessa");
        first.PostalCode.Should().Be("65000");
        first.AddressLine.Should().Be("literurna 12");
        first.Description.Should().Be("1");
        first.Verified.Should().BeFalse();
    }

    [Fact]
    public async Task TestCreateAddress()
    {
        StubPost("addresses", "addresses/create.json");

        var address = new Address
        {
            CityName = "New York",
            PostalCode = "123",
            AddressLine = "some street",
            Description = "test address",
            Country = Country.Build("1f6fc2bd-f081-4202-9b1a-d9cb88d942b9"),
            Identity = Identity.Build("some-identity-id")
        };

        var response = await Client.Addresses().CreateAsync(address);
        var created = response.Data;

        created.Id.Should().Be("bf69bc70-e1c2-442c-9f30-335ee299b663");
        created.CityName.Should().Be("New York");
        created.PostalCode.Should().Be("123");
        created.AddressLine.Should().Be("some street");
        created.Description.Should().Be("test address");
        created.Verified.Should().BeFalse();
    }

    [Fact]
    public async Task TestUpdateAddress()
    {
        StubPatch("addresses/bf69bc70-e1c2-442c-9f30-335ee299b663", "addresses/update.json");

        var address = Address.Build("bf69bc70-e1c2-442c-9f30-335ee299b663");
        address.CityName = "Chicago";
        address.PostalCode = "1234";
        address.AddressLine = "Main street";

        var response = await Client.Addresses().UpdateAsync(address);
        var updated = response.Data;

        updated.Id.Should().Be("bf69bc70-e1c2-442c-9f30-335ee299b663");
        updated.CityName.Should().Be("Chicago");
        updated.PostalCode.Should().Be("1234");
        updated.AddressLine.Should().Be("Main street");
        updated.Description.Should().Be("some address");
    }

    [Fact]
    public async Task TestDeleteAddress()
    {
        var id = "bf69bc70-e1c2-442c-9f30-335ee299b663";
        StubDelete("addresses/" + id);

        await Client.Addresses().DeleteAsync(id);
    }
}
