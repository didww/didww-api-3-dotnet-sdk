namespace Didww.Api3.Exception;

/// <summary>
/// An error answer of the JSON:API (a non-success status), with the parsed
/// JSON:API error objects when the body carries them.
/// </summary>
/// <remarks>
/// A 429 (<c>too_many_requests</c>) means the request budget of the account
/// for the current window is spent. The SDK does not retry it, as a write may
/// not be safe to send twice: wait <see cref="RetryAfter"/> before sending the
/// request again.
/// </remarks>
public class DidwwApiException : DidwwException
{
    public int HttpStatus { get; }
    public IReadOnlyList<ApiError> Errors { get; }

    /// <summary>
    /// The Retry-After of the answer, when given in seconds; null when it is
    /// missing or not a number of seconds.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    public DidwwApiException(int httpStatus, IList<ApiError> errors, TimeSpan? retryAfter = null)
        : base(BuildMessage(httpStatus, errors))
    {
        HttpStatus = httpStatus;
        Errors = errors?.AsReadOnly() ?? new List<ApiError>().AsReadOnly();
        RetryAfter = retryAfter;
    }

    public DidwwApiException(int httpStatus, string message, TimeSpan? retryAfter = null)
        : base(message)
    {
        HttpStatus = httpStatus;
        Errors = new List<ApiError>().AsReadOnly();
        RetryAfter = retryAfter;
    }

    private static string BuildMessage(int httpStatus, IList<ApiError>? errors)
    {
        var sb = new System.Text.StringBuilder($"DIDWW API error (HTTP {httpStatus})");
        if (errors != null && errors.Count > 0)
        {
            var parts = new List<string>();
            foreach (var error in errors)
            {
                var text = error.Detail ?? error.Title;
                if (text != null)
                    parts.Add(text);
            }

            if (parts.Count > 0)
            {
                sb.Append(": ");
                sb.Append(string.Join("; ", parts));
            }
        }
        return sb.ToString();
    }
}

public class ApiError
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public string? Status { get; set; }
    public string? Code { get; set; }
    public Dictionary<string, object>? Source { get; set; }
    public Dictionary<string, object>? Meta { get; set; }

    public override string ToString() => $"ApiError{{title='{Title}', detail='{Detail}'}}";
}
