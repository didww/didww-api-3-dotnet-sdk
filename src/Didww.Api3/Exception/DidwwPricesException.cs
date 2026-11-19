namespace Didww.Api3.Exception;

/// <summary>
/// The reason why a prices service call failed.
/// </summary>
public enum DidwwPricesErrorKind
{
    /// <summary>The request was refused (400), for example a filter value of the wrong format.</summary>
    BadRequest,

    /// <summary>The token was refused again after it was replaced once (401).</summary>
    NotAuthorized,

    /// <summary>The token does not allow the request (403).</summary>
    AccessDenied,

    /// <summary>The account has no price plan for the service (404 other than <c>price_not_found</c>).</summary>
    PricePlanNotAssigned,

    /// <summary>No price matches the lookup (404 <c>price_not_found</c>).</summary>
    PriceNotFound,

    /// <summary>The service cannot answer in the requested format (406).</summary>
    NotAcceptable,

    /// <summary>
    /// The request budget is spent (429). It is not retried;
    /// <see cref="DidwwPricesException.RetryAfter"/> says when to try again.
    /// </summary>
    TooManyRequests,

    /// <summary>
    /// The service cannot answer yet (503), for example a price file that is
    /// still being generated. <see cref="DidwwPricesException.RetryAfter"/> says
    /// when to try again.
    /// </summary>
    ServiceUnavailable,

    /// <summary>A status the call does not expect, such as a redirect, 202 or 204.</summary>
    UnexpectedStatus,

    /// <summary>
    /// The request failed in transport: connection, timeout, TLS or a broken
    /// response body. It is not retried.
    /// </summary>
    ConnectionError
}

/// <summary>
/// Raised by the prices services. Use <see cref="Kind"/> to tell the cases apart.
/// It never contains the API key or a token.
/// </summary>
public class DidwwPricesException : DidwwException
{
    /// <summary>The reason of the failure.</summary>
    public DidwwPricesErrorKind Kind { get; }

    /// <summary>HTTP status; null when no answer was received.</summary>
    public int? StatusCode { get; }

    /// <summary>The code of the first error object, for example <c>price_not_found</c>.</summary>
    public string? Code { get; }

    /// <summary>The error objects of the answer, if any.</summary>
    public IReadOnlyList<ApiError> Errors { get; }

    /// <summary>The Retry-After of the answer, when given in seconds.</summary>
    public TimeSpan? RetryAfter { get; }

    public DidwwPricesException(DidwwPricesErrorKind kind, int? statusCode = null, string? code = null,
        IReadOnlyList<ApiError>? errors = null, TimeSpan? retryAfter = null, System.Exception? innerException = null)
        : base(BuildMessage(statusCode, errors, innerException), innerException)
    {
        Kind = kind;
        StatusCode = statusCode;
        Code = code;
        Errors = errors ?? Array.Empty<ApiError>();
        RetryAfter = retryAfter;
    }

    private static string BuildMessage(int? statusCode, IReadOnlyList<ApiError>? errors,
        System.Exception? innerException)
    {
        var messages = new List<string>();
        if (errors != null)
        {
            foreach (var error in errors)
            {
                if (!string.IsNullOrEmpty(error.Detail))
                    messages.Add(error.Detail);
                else if (!string.IsNullOrEmpty(error.Title))
                    messages.Add(error.Title);
            }
        }

        if (messages.Count > 0)
            return "Prices service: " + string.Join("; ", messages);
        if (innerException != null)
            return "Prices service: " + innerException.Message;
        return $"Prices service responded with status {statusCode}";
    }
}
