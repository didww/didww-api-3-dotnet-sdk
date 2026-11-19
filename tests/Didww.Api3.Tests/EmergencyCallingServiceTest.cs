using Didww.Api3.Http;
using Didww.Api3.Resource.Enums;
using FluentAssertions;

namespace Didww.Api3.Tests;

public class EmergencyCallingServiceTest : BaseTest
{
    [Fact]
    public async Task TestListEmergencyCallingServices()
    {
        StubGet("emergency_calling_services", "emergency_calling_services/index.json");

        var response = await Client.EmergencyCallingServices().ListAsync();
        var services = response.Data;

        services.Should().NotBeEmpty();
        services.Should().HaveCount(1);

        var first = services[0];
        first.Id.Should().Be("11111111-2222-3333-4444-555555555555");
        first.Name.Should().Be("London Office ECS");
        first.Reference.Should().Be("ECS-0001");
        first.Status.Should().Be(EmergencyCallingServiceStatus.Active);
        first.ActivatedAt.Should().NotBeNull();
        first.CanceledAt.Should().BeNull();
        first.CreatedAt.Should().NotBeNull();
        first.RenewDate.Should().NotBeNull();
        first.Meta.Should().NotBeNull();
        first.Meta!["setup_price"]!.ToString().Should().Be("0.0");
        first.Meta!["monthly_price"]!.ToString().Should().Be("2.5");
    }

    [Fact]
    public async Task TestFindEmergencyCallingService()
    {
        StubGet("emergency_calling_services/01234567-89ab-cdef-0123-456789abcdef",
            "emergency_calling_services/show.json");

        var response = await Client.EmergencyCallingServices()
            .FindAsync("01234567-89ab-cdef-0123-456789abcdef");
        var service = response.Data;

        service.Id.Should().Be("01234567-89ab-cdef-0123-456789abcdef");
        service.Name.Should().Be("Berlin Office ECS");
        service.Reference.Should().Be("ECS-0042");
        service.Status.Should().Be(EmergencyCallingServiceStatus.PendingUpdate);
        service.NumberType.Should().Be(NumberType.National);
        service.Meta.Should().NotBeNull();
        service.Meta!["setup_price"]!.ToString().Should().Be("5.0");
        service.Meta!["monthly_price"]!.ToString().Should().Be("3.75");
    }

    [Fact]
    public async Task TestFindEmergencyCallingServiceWithAddress()
    {
        StubGet("emergency_calling_services/01234567-89ab-cdef-0123-456789abcdef",
            "emergency_calling_services/show_with_address.json");

        var queryParams = new QueryParams().Include("address");
        var response = await Client.EmergencyCallingServices()
            .FindAsync("01234567-89ab-cdef-0123-456789abcdef", queryParams);
        var service = response.Data;

        service.Id.Should().Be("01234567-89ab-cdef-0123-456789abcdef");
        service.Address.Should().NotBeNull();
        service.Address!.Id.Should().Be("b2c3d4e5-f6a7-8901-bcde-f12345678901");
        service.Address!.CityName.Should().Be("Berlin");
    }

    [Fact]
    public async Task TestFindEmergencyCallingServiceIncludesEmergencyRequirementAndVerification()
    {
        StubGet("emergency_calling_services/01234567-89ab-cdef-0123-456789abcdef",
            "emergency_calling_services/show.json");

        var response = await Client.EmergencyCallingServices()
            .FindAsync("01234567-89ab-cdef-0123-456789abcdef");
        var service = response.Data;

        service.EmergencyRequirement.Should().NotBeNull();
        service.EmergencyRequirement!.Id.Should().Be("44444444-3333-2222-1111-000000000000");
        service.EmergencyVerification.Should().NotBeNull();
        service.EmergencyVerification!.Id.Should().Be("77777777-6666-5555-4444-333333333333");
    }

    [Fact]
    public void TestStatusHelperActive()
    {
        var svc = new Didww.Api3.Resource.EmergencyCallingService { Status = EmergencyCallingServiceStatus.Active };
        svc.IsActive.Should().BeTrue();
        svc.IsCanceled.Should().BeFalse();
        svc.IsNew.Should().BeFalse();
    }

    [Fact]
    public void TestStatusHelperPendingUpdate()
    {
        var svc = new Didww.Api3.Resource.EmergencyCallingService { Status = EmergencyCallingServiceStatus.PendingUpdate };
        svc.IsPendingUpdate.Should().BeTrue();
        svc.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task TestDeleteEmergencyCallingService()
    {
        var id = "01234567-89ab-cdef-0123-456789abcdef";
        StubDelete("emergency_calling_services/" + id);

        await Client.EmergencyCallingServices().DeleteAsync(id);
    }
}
