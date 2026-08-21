namespace Cinedex.Application.Auth.RevokeAllSessions;

/// <summary>
/// Ends every session belonging to <paramref name="RequestingUserId"/>.
/// </summary>
/// <param name="RequestingUserId">
/// The subject of the caller's validated access token. There is deliberately no second field: with
/// nothing else to match on, no request input can widen the command past the caller who sent it.
/// </param>
public sealed record RevokeAllSessionsCommand(Guid RequestingUserId);
