using Newtonsoft.Json;

namespace Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;

/// <summary>
/// Polymorphic base for SmsTrunk.configuration (API 2026-10-22).
/// Concrete subtypes:
///   - sms_to_email: <see cref="SmsToEmailConfiguration"/>
///   - http_in:      <see cref="HttpInConfiguration"/>
///   - http_out:     <see cref="HttpOutConfiguration"/>
///   - smpp_esme:    <see cref="SmppEsmeConfiguration"/>
///   - smpp_smsc:    <see cref="SmppSmscConfiguration"/>
/// Unknown wire types are preserved via <see cref="GenericSmsTrunkConfiguration"/>
/// (forward-compat: round-trips through PATCH without data loss).
///
/// The configuration is sent as a whole object: null members are left out of
/// the request and keep their stored values, and on update the type must match
/// the stored one. A typed configuration therefore cannot clear a nullable
/// attribute (for example <c>body_type</c> and <c>body</c> of an <c>http_in</c>
/// switched to <c>get</c>, or <c>allowed_ips</c>). To send explicit nulls, use a
/// <see cref="GenericSmsTrunkConfiguration"/> of the same type:
/// <code>
/// trunk.Configuration = new GenericSmsTrunkConfiguration("http_in", new JObject
/// {
///     ["http_method"] = "get",
///     ["body_type"] = JValue.CreateNull(),
///     ["body"] = JValue.CreateNull()
/// });
/// </code>
/// </summary>
public abstract class SmsTrunkConfigurationBase
{
    [JsonIgnore]
    public abstract string ConfigurationType { get; }
}
