namespace Cinedex.Application.Exceptions;

/// <summary>
/// Thrown when authentication fails: bad credentials, or an invalid/expired/revoked refresh token.
/// Mapped to HTTP 401 in the presentation layer.
/// </summary>
public sealed class InvalidCredentialsException(string message) : Exception(message);