using Didww.Api3.Converter;
using Newtonsoft.Json;

namespace Didww.Api3.Resource.Enums;

/// <summary>
/// Category of a Voice OUT price row. (API 2026-10-22)
/// </summary>
[JsonConverter(typeof(TolerantStringEnumConverter))]
public enum VoiceOutPriceCategory
{
    [System.Runtime.Serialization.EnumMember(Value = "International")] International,
    [System.Runtime.Serialization.EnumMember(Value = "Origin based")] OriginBased,
    [System.Runtime.Serialization.EnumMember(Value = "Local")] Local
}
