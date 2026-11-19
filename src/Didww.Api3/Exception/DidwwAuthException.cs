namespace Didww.Api3.Exception;

/// <summary>
/// The reason why an access token could not be obtained.
/// </summary>
public enum DidwwAuthErrorKind
{
    /// <summary>The token service refused the API key (401). It is not retried.</summary>
    InvalidClient,

    /// <summary>The token service rejected the request (400), for example an unknown scope.</summary>
    InvalidRequest,

    /// <summary>
    /// The token service answered 429 (too many token requests). Further token
    /// requests fail without a request until <see cref="DidwwAuthException.RetryAfter"/>
    /// plus a small random delay has passed; such a refusal reports the time
    /// still left, so retry in a loop rather than once.
    /// </summary>
    RateLimited,

    /// <summary>The last attempt to reach the token service was answered with a 5xx.</summary>
    ServerError,

    /// <summary>The last attempt could not reach the token service, timed out, or TLS failed.</summary>
    ConnectionError,

    /// <summary>The granted token is empty or does not cover the requested audience.</summary>
    InvalidGrant,

    /// <summary>The token service answered with a status the SDK does not expect, such as a redirect.</summary>
    UnexpectedStatus
}

/// <summary>
/// Raised when an access token cannot be obtained from the token service.
/// It never contains the API key or a token. Use <see cref="Kind"/> to tell the
/// cases apart, for example
/// <c>catch (DidwwAuthException e) when (e.Kind == DidwwAuthErrorKind.RateLimited)</c>.
/// </summary>
public class DidwwAuthException : DidwwException
{
    /// <summary>The reason of the failure.</summary>
    public DidwwAuthErrorKind Kind { get; }

    /// <summary>HTTP status of the token service; null when no answer was received.</summary>
    public int? StatusCode { get; }

    /// <summary>The <c>error</c> field of the answer, for example <c>invalid_client</c>.</summary>
    public string? Error { get; }

    /// <summary>The <c>error_description</c> field of the answer.</summary>
    public string? ErrorDescription { get; }

    /// <summary>When to try again; set with <see cref="DidwwAuthErrorKind.RateLimited"/>.</summary>
    public TimeSpan? RetryAfter { get; }

    public DidwwAuthException(DidwwAuthErrorKind kind, int? statusCode = null, string? error = null,
        string? errorDescription = null, TimeSpan? retryAfter = null, System.Exception? innerException = null)
        : base(BuildMessage(statusCode, error, errorDescription, innerException), innerException)
    {
        Kind = kind;
        StatusCode = statusCode;
        Error = error;
        ErrorDescription = errorDescription;
        RetryAfter = retryAfter;
    }

    private static string BuildMessage(int? statusCode, string? error, string? errorDescription,
        System.Exception? innerException)
    {
        const string prefix = "Token service: ";
        if (!string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(errorDescription))
            return prefix + error + ": " + errorDescription;
        if (!string.IsNullOrEmpty(error))
            return prefix + error;
        if (!string.IsNullOrEmpty(errorDescription))
            return prefix + errorDescription;
        if (innerException != null)
            return prefix + innerException.Message;
        return $"Token service responded with status {statusCode}";
    }
}
