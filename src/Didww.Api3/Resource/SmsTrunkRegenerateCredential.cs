using Newtonsoft.Json;

namespace Didww.Api3.Resource;

/// <summary>
/// Rotates the generated credentials of an SMS trunk (API 2026-10-22). Only
/// <c>smpp_esme</c> and <c>http_out</c> trunks have credentials to rotate. The
/// response carries no credentials: read the trunk again to get the new ones.
/// <code>
/// await client.SmsTrunkRegenerateCredentials().CreateAsync(
///     new SmsTrunkRegenerateCredential { SmsTrunk = SmsTrunk.Build(trunkId) });
/// </code>
/// </summary>
public class SmsTrunkRegenerateCredential : BaseResource
{
    public override string Type => "sms_trunk_regenerate_credentials";

    private SmsTrunk? _smsTrunk;
    /// <summary>The trunk whose credentials to rotate.</summary>
    [JsonProperty("sms_trunk")]
    public SmsTrunk? SmsTrunk
    {
        get => _smsTrunk;
        set => SetProperty(ref _smsTrunk, value);
    }
}
