using Didww.Api3.Resource;
using Didww.Api3.Resource.Enums;
using Didww.Api3.Resource.OrderItem;
using FluentAssertions;
using JsonApiSerializer;
using Newtonsoft.Json;

namespace Didww.Api3.Tests;

public class OrderTest : BaseTest
{
    [Fact]
    public async Task TestFindOrder()
    {
        StubGet("orders/9df11dac-9d83-448c-8866-19c998be33db", "orders/show.json");

        var response = await Client.Orders().FindAsync("9df11dac-9d83-448c-8866-19c998be33db");
        var order = response.Data;

        order.Id.Should().Be("9df11dac-9d83-448c-8866-19c998be33db");
        order.Status.Should().Be(OrderStatus.Completed);
        order.Description.Should().Be("Payment processing fee");
        order.Reference.Should().Be("SPT-474057");
        order.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task TestCreateOrder()
    {
        StubPost("orders", "orders/create_request.json", "orders/create.json");

        var item1 = new DidOrderItem { SkuId = "acc46374-0b34-4912-9f67-8340339db1e5", Qty = 2 };
        var item2 = new DidOrderItem { SkuId = "f36d2812-2195-4385-85e8-e59c3484a8bc", Qty = 1 };

        var order = new Order
        {
            AllowBackOrdering = true,
            Items = new List<OrderItemBase> { item1, item2 }
        };

        var response = await Client.Orders().CreateAsync(order);
        var created = response.Data;

        created.Id.Should().Be("5da18706-be9f-49b0-aeec-0480aacd49ad");
        created.Status.Should().Be(OrderStatus.Pending);
        created.Description.Should().Be("DID");
        created.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task TestOrderBillingCyclesCountSave()
    {
        StubPost("orders", "orders/create_billing_cycles.json");

        var item = new DidOrderItem
        {
            SkuId = "f36d2812-2195-4385-85e8-e59c3484a8bc",
            Qty = 1,
            BillingCyclesCount = 5
        };

        var order = new Order
        {
            AllowBackOrdering = true,
            Items = new List<OrderItemBase> { item }
        };

        var response = await Client.Orders().CreateAsync(order);
        var created = response.Data;

        created.Id.Should().Be("9b9f2121-8d9e-4aa8-9754-dbaf6f695fd6");
        created.Status.Should().Be(OrderStatus.Pending);
        created.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task TestOrderAvailableDidSave()
    {
        StubPost("orders", "orders/create_available_did.json");

        var item = new AvailableDidOrderItem
        {
            SkuId = "acc46374-0b34-4912-9f67-8340339db1e5",
            AvailableDidId = "c43441e3-82d4-4d84-93e2-80998576c1ce"
        };

        var order = new Order
        {
            Items = new List<OrderItemBase> { item }
        };

        var response = await Client.Orders().CreateAsync(order);
        var created = response.Data;

        created.Id.Should().Be("9b9f2121-8d9e-4aa8-9754-dbaf6f695fd6");
        created.Status.Should().Be(OrderStatus.Pending);
        created.Items.Should().HaveCount(1);
        created.Items![0].Should().BeOfType<DidOrderItem>();
    }

    [Fact]
    public async Task TestOrderReservationSave()
    {
        StubPost("orders", "orders/create_reservation.json");

        var item = new ReservationDidOrderItem
        {
            SkuId = "32840f64-5c3f-4278-8c8d-887fbe2f03f4",
            DidReservationId = "e3ed9f97-1058-430c-9134-38f1c614ee9f"
        };

        var order = new Order
        {
            Items = new List<OrderItemBase> { item }
        };

        var response = await Client.Orders().CreateAsync(order);
        var created = response.Data;

        created.Id.Should().Be("a9a7ff2d-d634-4545-bf28-dfda92d1c723");
        created.Status.Should().Be(OrderStatus.Pending);
        created.Items.Should().HaveCount(1);
        created.Items![0].Should().BeOfType<DidOrderItem>();
    }

    [Fact]
    public async Task TestOrderCapacitySave()
    {
        StubPost("orders", "orders/create_capacity.json");

        var item = new CapacityOrderItem
        {
            CapacityPoolId = "b7522a31-4bf3-4c23-81e8-e7a14b23663f",
            Qty = 1
        };

        var order = new Order
        {
            Items = new List<OrderItemBase> { item }
        };

        var response = await Client.Orders().CreateAsync(order);
        var created = response.Data;

        created.Id.Should().Be("68a46dd5-d405-4283-b7a5-62503267e9f8");
        created.Status.Should().Be(OrderStatus.Completed);
        created.Description.Should().Be("Capacity");
        created.Items.Should().HaveCount(1);
        created.Items![0].Should().BeOfType<CapacityOrderItem>();
    }

    [Fact]
    public async Task TestCreateOrderWithNanpaPrefix()
    {
        StubPost("orders", "orders/create_nanpa.json");

        var item = new DidOrderItem
        {
            SkuId = "fe77889c-f05a-40ad-a845-96aca3c28054",
            NanpaPrefixId = "eeed293b-f3d8-4ef8-91ef-1b077d174b3b",
            Qty = 1
        };

        var order = new Order
        {
            AllowBackOrdering = true,
            Items = new List<OrderItemBase> { item }
        };

        var response = await Client.Orders().CreateAsync(order);
        var created = response.Data;

        created.Id.Should().Be("c617f0ff-f819-477f-a17b-a8d248c4443e");
        created.Status.Should().Be(OrderStatus.Pending);
        created.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task TestFindOrderWithGenericItem()
    {
        StubGet("orders/9df11dac-9d83-448c-8866-19c998be33db", "orders/show_generic_item.json");

        var response = await Client.Orders().FindAsync("9df11dac-9d83-448c-8866-19c998be33db");
        var order = response.Data;

        order.Id.Should().Be("9df11dac-9d83-448c-8866-19c998be33db");
        order.Status.Should().Be(OrderStatus.Completed);
        order.Items.Should().HaveCount(1);
        order.Items![0].Should().BeOfType<GenericOrderItem>();
    }

    [Fact]
    public async Task TestOrderSkuSaveWithCallback()
    {
        StubPost("orders", "orders_with_callback/create_request.json", "orders_with_callback/create.json");

        var item = new DidOrderItem
        {
            SkuId = "f36d2812-2195-4385-85e8-e59c3484a8bc",
            Qty = 1
        };

        var order = new Order
        {
            AllowBackOrdering = true,
            CallbackUrl = "https://example.com/callback",
            CallbackMethod = CallbackMethod.Post,
            Items = new List<OrderItemBase> { item }
        };

        var response = await Client.Orders().CreateAsync(order);
        var created = response.Data;

        created.Id.Should().Be("5da18706-be9f-49b0-aeec-0480aacd49ad");
        created.Status.Should().Be(OrderStatus.Pending);
        created.CallbackUrl.Should().Be("https://example.com/callback");
        created.CallbackMethod.Should().Be(CallbackMethod.Post);
        created.Items.Should().HaveCount(1);
    }

    [Fact]
    public void TestSerializeNullItems()
    {
        var order = new Order
        {
            Items = null
        };

        var settings = new JsonApiSerializerSettings
        {
            NullValueHandling = NullValueHandling.Include
        };
        var json = JsonConvert.SerializeObject(order, settings);
        // Should not throw, items serialized as null
        json.Should().NotBeNull();
    }
}
