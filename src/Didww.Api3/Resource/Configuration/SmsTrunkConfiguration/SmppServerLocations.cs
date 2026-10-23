namespace Didww.Api3.Resource.Configuration.SmsTrunkConfiguration;

/// <summary>
/// Locations of the DIDWW SMPP servers an <c>smpp_esme</c> or <c>smpp_smsc</c>
/// trunk is served from. Production accepts both; the sandbox accepts
/// <see cref="Nyc"/> only. (API 2026-10-22)
/// </summary>
public static class SmppServerLocations
{
    public const string Nyc = "nyc";
    public const string Ams = "ams";
}
