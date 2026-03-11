using Newtonsoft.Json;

namespace Didww.Api3.Converter;

public class SemicolonSplitConverter : JsonConverter<string[]?>
{
    public override string[]? ReadJson(JsonReader reader, Type objectType, string[]? existingValue,
        bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var raw = (string?)reader.Value;
        if (raw == null)
            return null;

        return raw.Split(new[] { "; " }, StringSplitOptions.None);
    }

    public override void WriteJson(JsonWriter writer, string[]? value, JsonSerializer serializer)
    {
        if (value == null)
            writer.WriteNull();
        else
            writer.WriteValue(string.Join("; ", value));
    }
}
