namespace Didww.Api3.Exception;

/// <summary>
/// A failure on the client side, such as an unexpected response shape, a
/// refused export URL or a broken download.
/// </summary>
public class DidwwClientException : DidwwException
{
    public DidwwClientException(string message) : base(message) { }
    public DidwwClientException(string message, System.Exception innerException) : base(message, innerException) { }
}
