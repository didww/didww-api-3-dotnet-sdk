using Didww.Api3.Converter;
using Didww.Api3.Resource.Enums;
using Didww.Api3.Resource.OrderItem;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class Order : BaseResource
{
    public override string Type => "orders";

    public static Order Build(string id) => BaseResource.Build<Order>(id);

    [JsonProperty("amount")]
    public decimal? Amount { get; set; }

    [JsonProperty("status")]
    public OrderStatus? Status { get; set; }

    [JsonProperty("description")]
    public string? Description { get; set; }

    [JsonProperty("reference")]
    public string? Reference { get; set; }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    private string? _callbackUrl;
    [JsonProperty("callback_url")]
    public string? CallbackUrl
    {
        get => _callbackUrl;
        set => _callbackUrl = MarkDirty("callbackUrl", value);
    }

    private CallbackMethod? _callbackMethod;
    [JsonProperty("callback_method")]
    public CallbackMethod? CallbackMethod
    {
        get => _callbackMethod;
        set => _callbackMethod = MarkDirty("callbackMethod", value);
    }

    private bool? _allowBackOrdering;
    [JsonProperty("allow_back_ordering")]
    public bool? AllowBackOrdering
    {
        get => _allowBackOrdering;
        set => _allowBackOrdering = MarkDirty("allowBackOrdering", value);
    }

    private List<OrderItemBase>? _items;
    [JsonProperty("items")]
    [JsonConverter(typeof(OrderItemConverter))]
    public List<OrderItemBase>? Items
    {
        get => _items;
        set => _items = MarkDirty("items", value);
    }
}
