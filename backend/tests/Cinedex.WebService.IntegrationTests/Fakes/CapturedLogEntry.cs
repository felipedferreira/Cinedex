using Microsoft.Extensions.Logging;

namespace Cinedex.WebService.IntegrationTests.Fakes;

internal sealed record CapturedLogEntry(
    string Category,
    LogLevel Level,
    EventId EventId,
    string Message,
    IReadOnlyList<KeyValuePair<string, object?>> State);
