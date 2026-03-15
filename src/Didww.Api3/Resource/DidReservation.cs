using Newtonsoft.Json;

namespace Didww.Api3.Resource;

public class DidReservation : BaseResource
{
    public override string Type => "did_reservations";
    public static DidReservation Build(string id) => BaseResource.Build<DidReservation>(id);

    private string? _description;
    [JsonProperty("description")]
    public string? Description { get => _description; set => _description = MarkDirty("description", value); }

    [JsonProperty("expire_at")] public DateTimeOffset? ExpireAt { get; set; }
    [JsonProperty("created_at")] public DateTimeOffset? CreatedAt { get; set; }

    private AvailableDid? _availableDid;
    [JsonProperty("available_did")]
    public AvailableDid? AvailableDid { get => _availableDid; set => _availableDid = MarkDirty("availableDid", value); }
}
