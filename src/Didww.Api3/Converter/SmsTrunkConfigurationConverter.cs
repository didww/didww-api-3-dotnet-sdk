using Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Didww.Api3.Converter;

/// <summary>
/// JSON:API-style polymorphic converter for SmsTrunk.configuration
/// (API 2026-10-22). Reads and writes <c>{"type": ..., "attributes": {...}}</c>.
/// Unknown wire types are wrapped in <see cref="GenericSmsTrunkConfiguration"/>
/// and written back verbatim, explicit nulls included. Read-only attributes of
/// the typed configurations (the generated credentials of <c>http_out</c> and
/// <c>smpp_esme</c>) are left out of request bodies.
/// </summary>
public class SmsTrunkConfigurationConverter : JsonConverter<SmsTrunkConfigurationBase>
{
    private static readonly Dictionary<string, Type> TypeMap = new()
    {
        ["sms_to_email"] = typeof(SmsToEmailConfiguration),
        ["http_in"] = typeof(HttpInConfiguration),
        ["http_out"] = typeof(HttpOutConfiguration),
        ["smpp_esme"] = typeof(SmppEsmeConfiguration),
        ["smpp_smsc"] = typeof(SmppSmscConfiguration),
    };

    private static readonly Dictionary<Type, string[]> ReadOnlyAttributes = new()
    {
        [typeof(HttpOutConfiguration)] = ["auth_name", "auth_password"],
        [typeof(SmppEsmeConfiguration)] = ["system_id", "password"],
    };

    public override SmsTrunkConfigurationBase? ReadJson(JsonReader reader, Type objectType,
        SmsTrunkConfigurationBase? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);
        if (token.Type == JTokenType.Null)
            return null;

        var typeValue = token["type"]?.ToString();
        if (typeValue == null)
            throw new JsonSerializationException("Missing 'type' field in SMS trunk configuration");

        var attributes = token["attributes"] as JObject ?? new JObject();

        if (TypeMap.TryGetValue(typeValue, out var clazz))
            return (SmsTrunkConfigurationBase?)attributes.ToObject(clazz, serializer);

        // Forward-compat: preserve the unknown type and attributes.
        return new GenericSmsTrunkConfiguration(typeValue, attributes);
    }

    public override void WriteJson(JsonWriter writer, SmsTrunkConfigurationBase? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (value is GenericSmsTrunkConfiguration generic)
        {
            new JObject
            {
                ["type"] = generic.ConfigurationType,
                ["attributes"] = generic.Attributes.DeepClone()
            }.WriteTo(writer);
            return;
        }

        var wrapper = PolymorphicJsonHelper.Serialize(value, value.ConfigurationType, serializer);
        if (ReadOnlyAttributes.TryGetValue(value.GetType(), out var readOnly) &&
            wrapper["attributes"] is JObject attributes)
        {
            foreach (var name in readOnly)
                attributes.Remove(name);
        }

        wrapper.WriteTo(writer);
    }
}
