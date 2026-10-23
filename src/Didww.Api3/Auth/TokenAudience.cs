namespace Didww.Api3.Auth;

/// <summary>
/// Token audiences: the production hostname of each service, in every
/// environment and with any URL override.
/// </summary>
internal static class TokenAudience
{
    internal const string Api = "api.didww.com";
    internal const string VoiceIn = "api-vin.didww.com";
    internal const string VoiceOut = "api-vout.didww.com";
    internal const string Sms = "api-sms.didww.com";
}
