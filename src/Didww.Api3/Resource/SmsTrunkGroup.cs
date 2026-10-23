using System.Globalization;
using Newtonsoft.Json;

namespace Didww.Api3.Resource;

/// <summary>
/// Group of SMS trunks (API 2026-10-22). Deleting a group also deletes its trunks.
/// </summary>
public class SmsTrunkGroup : BaseResource
{
    public override string Type => "sms_trunk_groups";

    public static SmsTrunkGroup Build(string id) => BaseResource.Build<SmsTrunkGroup>(id);

    private string? _name;
    /// <summary>Name of the group. Required on create.</summary>
    [JsonProperty("name")]
    public string? Name { get => _name; set => SetProperty(ref _name, value); }

    private string? _externalReferenceId;
    /// <summary>Customer's own identifier for the group, up to 100 characters.</summary>
    [JsonProperty("external_reference_id")]
    public string? ExternalReferenceId { get => _externalReferenceId; set => SetProperty(ref _externalReferenceId, value); }

    [JsonProperty("created_at")]
    public DateTimeOffset? CreatedAt { get; set; }

    [JsonProperty("updated_at")]
    public DateTimeOffset? UpdatedAt { get; set; }

    private List<SmsTrunk>? _smsTrunks;
    /// <summary>
    /// Trunks of the group. The list replaces the stored membership; assign an
    /// empty list to remove every trunk from the group.
    /// </summary>
    [JsonProperty("sms_trunks")]
    public List<SmsTrunk>? SmsTrunks { get => _smsTrunks; set => SetProperty(ref _smsTrunks, value); }

    /// <summary>DIDs whose inbound messages the group handles. Read-only.</summary>
    [JsonProperty("incoming_dids")]
    public List<Did>? IncomingDids { get; set; }

    /// <summary>Resource-level meta. Contains <c>sms_trunks_count</c>.</summary>
    [JsonProperty("meta")]
    public Dictionary<string, object>? Meta { get; set; }

    /// <summary>Number of trunks currently in the group, from <see cref="Meta"/>.</summary>
    [JsonIgnore]
    public int? SmsTrunksCount =>
        Meta != null && Meta.TryGetValue("sms_trunks_count", out var count) && count is IConvertible convertible
            ? convertible.ToInt32(CultureInfo.InvariantCulture)
            : null;
}
