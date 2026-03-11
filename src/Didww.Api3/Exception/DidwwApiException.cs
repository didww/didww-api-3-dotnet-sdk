namespace Didww.Api3.Exception;

public class DidwwApiException : System.Exception
{
    public int HttpStatus { get; }
    public IReadOnlyList<ApiError> Errors { get; }

    public DidwwApiException(int httpStatus, IList<ApiError> errors)
        : base(BuildMessage(httpStatus, errors))
    {
        HttpStatus = httpStatus;
        Errors = errors?.AsReadOnly() ?? new List<ApiError>().AsReadOnly();
    }

    public DidwwApiException(int httpStatus, string message)
        : base(message)
    {
        HttpStatus = httpStatus;
        Errors = new List<ApiError>().AsReadOnly();
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
