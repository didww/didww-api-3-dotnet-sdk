using Didww.Api3.Internal;
using Newtonsoft.Json;

namespace Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;

/// <summary>
/// DIDWW binds to the customer's SMSC as an SMPP client. <see cref="Host"/>,
/// <see cref="SystemId"/>, <see cref="Password"/> and
/// <see cref="ServerLocation"/> are required when the trunk is created;
/// <see cref="TxPort"/> defaults to 2775 and <see cref="RxPort"/> to 2776.
/// (API 2026-10-22)
/// </summary>
public class SmppSmscConfiguration : SmsTrunkConfigurationBase
{
    [JsonIgnore]
    public override string ConfigurationType => "smpp_smsc";

    [JsonProperty("host")]
    public string? Host { get; set; }

    [JsonProperty("tx_port")]
    public int? TxPort { get; set; }

    [JsonProperty("rx_port")]
    public int? RxPort { get; set; }

    [JsonProperty("transceiver_mode")]
    public bool? TransceiverMode { get; set; }

    /// <summary>Bind timeout, seconds.</summary>
    [JsonProperty("connection_timeout")]
    public int? ConnectionTimeout { get; set; }

    [JsonProperty("use_ssl")]
    public bool? UseSsl { get; set; }

    /// <summary>SMPP system_id DIDWW binds with, up to 15 characters.</summary>
    [JsonProperty("system_id")]
    public string? SystemId { get; set; }

    /// <summary>SMPP system_type, up to 12 characters.</summary>
    [JsonProperty("system_type")]
    public string? SystemType { get; set; }

    /// <summary>SMPP bind password, up to 8 characters.</summary>
    [JsonProperty("password")]
    public string? Password { get; set; }

    /// <summary>Location of the DIDWW SMPP server, see <see cref="SmppServerLocations"/>.</summary>
    [JsonProperty("server_location")]
    public string? ServerLocation { get; set; }

    // The default ToString redacts the bind password. The wire payload is
    // unaffected.
    public override string ToString()
    {
        return $"SmppSmscConfiguration(Host={Host ?? "null"}, TxPort={TxPort?.ToString() ?? "null"}, RxPort={RxPort?.ToString() ?? "null"}, " +
               $"TransceiverMode={TransceiverMode?.ToString() ?? "null"}, ConnectionTimeout={ConnectionTimeout?.ToString() ?? "null"}, " +
               $"UseSsl={UseSsl?.ToString() ?? "null"}, SystemId={SystemId ?? "null"}, SystemType={SystemType ?? "null"}, " +
               $"Password={Redact.Mask(Password)}, ServerLocation={ServerLocation ?? "null"})";
    }
}
