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
            sb.Append(": ");
            for (int i = 0; i < errors.Count; i++)
            {
                if (i > 0)
                    sb.Append("; ");
                sb.Append(errors[i].Detail ?? errors[i].Title);
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
