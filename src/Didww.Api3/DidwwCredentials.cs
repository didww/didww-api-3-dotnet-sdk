using Didww.Api3.Internal;

namespace Didww.Api3;

public class DidwwCredentials
{
    /// <summary>
    /// The API key. It is sent to the token service only, which exchanges it
    /// for short-lived access tokens.
    /// </summary>
    public string ApiKey { get; }
    public DidwwEnvironment Environment { get; }

    public DidwwCredentials(string apiKey, DidwwEnvironment environment)
    {
        ApiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        Environment = environment;
    }

    public string GetBaseUrl() => Environment.GetBaseUrl();

    /// <summary>Describes the credentials without the API key.</summary>
    public override string ToString() => $"DidwwCredentials{{ApiKey: {Redact.Mask(ApiKey)}, Environment: {Environment}}}";
}
