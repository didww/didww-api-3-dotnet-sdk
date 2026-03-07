using Didww.Api3.Resource;
using Didww.Api3.Resource.OrderItem;

namespace Didww.Api3.Examples;

public static class OrdersCapacityExample
{
    public static async Task RunAsync(DidwwClient client)
    {
        Console.WriteLine("--- Order Capacity ---");

        // Get capacity pools
        var pools = await client.CapacityPools().ListAsync();
        if (pools.Data.Count == 0)
        {
            Console.WriteLine("  No capacity pools found, skipping.");
            return;
        }

        var pool = pools.Data[0];
        Console.WriteLine($"  Capacity pool: {pool.Name}");

        var orderItem = new CapacityOrderItem
        {
            CapacityPoolId = pool.Id,
            Qty = 1
        };

        var order = new Order
        {
            Items = new List<OrderItemBase> { orderItem }
        };

        var response = await client.Orders().CreateAsync(order);
        Console.WriteLine($"  Order created: {response.Data.Id} (Status: {response.Data.Status}, Amount: {response.Data.Amount})");
    }
}
