namespace Didww.Api3;

public enum DidwwEnvironment
{
    Production,
    Sandbox
}

public static class DidwwEnvironmentExtensions
{
    /// <summary>The JSON:API base URL of the environment.</summary>
    public static string GetBaseUrl(this DidwwEnvironment environment) => environment switch
    {
        DidwwEnvironment.Production => "https://api.didww.com/v3",
        DidwwEnvironment.Sandbox => "https://sandbox-api.didww.com/v3",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    /// <summary>The token service base URL of the environment.</summary>
    public static string GetAuthUrl(this DidwwEnvironment environment) => environment switch
    {
        DidwwEnvironment.Production => "https://auth.didww.com",
        DidwwEnvironment.Sandbox => "https://auth-sandbox.didww.com",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    /// <summary>The Voice IN prices base URL of the environment.</summary>
    public static string GetVoiceInPricesUrl(this DidwwEnvironment environment) => environment switch
    {
        DidwwEnvironment.Production => "https://api-vin.didww.com/v3",
        DidwwEnvironment.Sandbox => "https://api-vin-sandbox.didww.com/v3",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    /// <summary>The Voice OUT prices base URL of the environment.</summary>
    public static string GetVoiceOutPricesUrl(this DidwwEnvironment environment) => environment switch
    {
        DidwwEnvironment.Production => "https://api-vout.didww.com/v3",
        DidwwEnvironment.Sandbox => "https://api-vout-sandbox.didww.com/v3",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };

    /// <summary>The SMS prices base URL of the environment.</summary>
    public static string GetSmsPricesUrl(this DidwwEnvironment environment) => environment switch
    {
        DidwwEnvironment.Production => "https://api-sms.didww.com/v3",
        DidwwEnvironment.Sandbox => "https://api-sms-sandbox.didww.com/v3",
        _ => throw new ArgumentOutOfRangeException(nameof(environment))
    };
}
