namespace Cinedex.Application.Email;

/// <summary>
/// An email recipient: an address and an optional display name.
/// </summary>
/// <param name="Address">The email address.</param>
/// <param name="DisplayName">An optional display name shown by mail clients.</param>
public sealed record EmailRecipient(string Address, string? DisplayName = null);