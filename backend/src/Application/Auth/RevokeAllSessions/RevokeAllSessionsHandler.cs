using Cinedex.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Cinedex.Application.Auth.RevokeAllSessions;

internal sealed class RevokeAllSessionsHandler(
    ITokenService tokenService,
    ILogger<RevokeAllSessionsHandler> logger) : IRevokeAllSessionsHandler
{
    private static readonly EventId AllSessionsRevoked = new(1003, "AllSessionsRevoked");

    public async Task HandleAsync(RevokeAllSessionsCommand command, CancellationToken cancellationToken)
    {
        var revokedCount = await tokenService.RevokeAllSessionsAsync(
            command.RequestingUserId,
            cancellationToken);

        // Logged on every call, including the ones that revoke nothing — unlike the logout path,
        // which stays silent unless something is wrong. Signing out everywhere is what somebody does
        // when they think they have been compromised, so the fact that they asked is itself the
        // record an incident-response runbook needs, and a zero says the panic button was pressed
        // twice rather than that nothing happened.
        //
        // Information, not Warning: this is a user exercising a control that works, not an anomaly.
        // 1001 (reuse detected) and 1002 (logout with another user's token) are the anomalies.
        //
        // No identifiers in the message or the state, matching 1001 and 1002. The trace context
        // correlates the event back to the request — and from there to the caller — without the log
        // store itself becoming a record of who was signed in and when.
        logger.LogInformation(
            AllSessionsRevoked,
            "All sessions revoked at the account holder's request; {RevokedTokenCount} active token(s) ended.",
            revokedCount);
    }
}
