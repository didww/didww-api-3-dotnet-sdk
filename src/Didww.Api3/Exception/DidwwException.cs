namespace Didww.Api3.Exception;

/// <summary>
/// Base class of every exception raised by the SDK. Messages never contain the
/// API key or an access token.
/// </summary>
public abstract class DidwwException : System.Exception
{
    protected DidwwException(string message) : base(message) { }

    protected DidwwException(string message, System.Exception? innerException) : base(message, innerException) { }
}
