namespace Didww.Api3.Auth;

/// <summary>
/// The scope requested for access tokens. Without one the token service
/// grants full access.
/// </summary>
public enum DidwwScope
{
    /// <summary>Read-only tokens: every method but GET and HEAD is refused.</summary>
    Read,

    /// <summary>Tokens allowing every method.</summary>
    Full
}
