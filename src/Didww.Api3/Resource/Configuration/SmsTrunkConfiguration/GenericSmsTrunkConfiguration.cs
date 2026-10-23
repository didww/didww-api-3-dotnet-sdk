using Didww.Api3.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;

/// <summary>
/// Configuration of a type this SDK version does not know yet, or a known type
/// whose attributes must be sent verbatim. The wire type and attributes are
/// kept as received and written back unchanged, explicit nulls included, so it
/// also serves to clear nullable attributes (see
/// <see cref="SmsTrunkConfigurationBase"/>). Numbers keep their exact value.
/// </summary>
public class GenericSmsTrunkConfiguration : SmsTrunkConfigurationBase
{
    private readonly string _storedType;

    [JsonIgnore]
    public override string ConfigurationType => _storedType;

    [JsonProperty("attributes")]
    public JObject Attributes { get; }

    public GenericSmsTrunkConfiguration(string storedType, JObject attributes)
    {
        ArgumentNullException.ThrowIfNull(storedType);
        ArgumentNullException.ThrowIfNull(attributes);
        _storedType = storedType;
        Attributes = attributes;
    }

    // Attribute values may carry credentials, so the default ToString shows
    // only the attribute names and explicit nulls. The wire payload is
    // unaffected.
    public override string ToString()
    {
        var attributes = Attributes.Properties()
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}={(p.Value.Type == JTokenType.Null ? "null" : Redact.Mask(p.Value.ToString(Formatting.None)))}");
        return $"GenericSmsTrunkConfiguration(Type={_storedType}, Attributes={{{string.Join(", ", attributes)}}})";
    }
}
