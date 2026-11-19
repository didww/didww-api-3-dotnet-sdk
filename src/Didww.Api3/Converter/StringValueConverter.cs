using Newtonsoft.Json;

namespace Didww.Api3.Converter;

/// <summary>
/// A value the API sends as a string and may extend with new strings.
/// </summary>
internal interface IStringValue
{
    string Value { get; }
}

/// <summary>
/// Reads and writes an <see cref="IStringValue"/> as its JSON string. Any
/// string is read, so a value the server adds later is kept and written back
/// unchanged. A JSON <c>null</c> reads as <c>null</c> for a nullable property
/// and as the default value otherwise, which is written back as <c>null</c>.
/// </summary>
internal class StringValueConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) =>
        typeof(IStringValue).IsAssignableFrom(Nullable.GetUnderlyingType(objectType) ?? objectType);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var valueType = Nullable.GetUnderlyingType(objectType);
        return reader.TokenType switch
        {
            JsonToken.Null => valueType is null ? Activator.CreateInstance(objectType) : null,
            JsonToken.String => Activator.CreateInstance(valueType ?? objectType, (string)reader.Value!),
            _ => throw new JsonSerializationException($"Unexpected token {reader.TokenType} for {(valueType ?? objectType).Name}, expected a string."),
        };
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer) =>
        writer.WriteValue(((IStringValue?)value)?.Value);
}
