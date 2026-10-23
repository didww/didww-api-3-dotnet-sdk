using Newtonsoft.Json;

namespace Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;

/// <summary>
/// Inbound messages are forwarded as email. <see cref="SendTo"/>,
/// <see cref="Subject"/> and <see cref="Message"/> are required when the
/// trunk is created. (API 2026-10-22)
/// </summary>
public class SmsToEmailConfiguration : SmsTrunkConfigurationBase
{
    [JsonIgnore]
    public override string ConfigurationType => "sms_to_email";

    [JsonProperty("send_to")]
    public string? SendTo { get; set; }

    [JsonProperty("subject")]
    public string? Subject { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }
}
