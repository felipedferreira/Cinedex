namespace Cinedex.Application.Exceptions;

/// <summary>
/// Represents a failure while handing an email to its configured delivery provider.
/// </summary>
public sealed class EmailDeliveryException(string message, Exception innerException)
    : Exception(message, innerException);
