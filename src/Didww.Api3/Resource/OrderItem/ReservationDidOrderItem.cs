using Newtonsoft.Json;

namespace Didww.Api3.Resource.OrderItem;

public class ReservationDidOrderItem : DidOrderItem
{
    [JsonProperty("did_reservation_id")]
    public string? DidReservationId { get; set; }
}
