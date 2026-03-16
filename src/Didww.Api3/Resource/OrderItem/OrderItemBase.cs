using Newtonsoft.Json;

namespace Didww.Api3.Resource.OrderItem;

public abstract class OrderItemBase
{
    [JsonIgnore]
    public abstract string ItemType { get; }
}
