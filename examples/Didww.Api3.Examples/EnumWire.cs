using Newtonsoft.Json;

namespace Didww.Api3.Examples;

// Converts an SDK enum value to the string the API uses for it, for filters
// such as Filter("number_type", EnumWire.Value(NumberType.TollFree)) -> "toll_free".
public static class EnumWire
{
    public static string Value<T>(T value) where T : struct, Enum =>
        JsonConvert.DeserializeObject<string>(JsonConvert.SerializeObject(value))!;
}
