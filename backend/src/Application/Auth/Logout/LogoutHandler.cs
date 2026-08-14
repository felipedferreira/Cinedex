using Cinedex.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Auth.Logout;

internal sealed class LogoutHandler(
    ITokenService tokenService,
    ILogger<LogoutHandler> logger) : ILogoutHandler
{
    private static readonly EventId RefreshTokenOwnershipMismatch = new(1002, "RefreshTokenOwnershipMismatch");

    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken)
    {
        // Ownership is a condition of the revocation, not a question asked before it, so nothing can
        // change between the two. The happy path is this one call.
        var revokedCount = await tokenService.RevokeSessionAsync(
            command.RequestingUserId,
            command.RefreshToken,
            cancellationToken);

        if (revokedCount > 0)
        {
            return;
        }

        // Nothing matched, which is usually unremarkable: a second logout, or a session an earlier
        // reuse response already burned. One case is not unremarkable — a token that exists and
        // belongs to somebody else is the only point in the service where a caller demonstrably
        // holds another user's refresh token, and the same evidence makes RefreshAsync revoke a
        // whole family. Attributing it costs a read, but only ever off the happy path, and the
        // outcome is already settled: this decides how loudly to say what happened, nothing more.
        var belongsToAnotherUser = await tokenService.RefreshTokenBelongsToAnotherUserAsync(
            command.RequestingUserId,
            command.RefreshToken,
            cancellationToken);

        if (belongsToAnotherUser)
        {
            // No identifiers in the message, matching how the rest of this subsystem logs: the trace
            // context correlates the request without the log store becoming a record of who was
            // signed in. The EventId is what makes it queryable.
            logger.LogWarning(
                RefreshTokenOwnershipMismatch,
                "A logout presented a refresh token issued to another user; nothing was revoked.");
        }
    }
}
