using Microsoft.Extensions.Logging;

namespace FoundryOceanus.Observability.OpenTelemetry.Tests.Fakes;

/// <summary>
/// One log entry recorded by <see cref="CapturingLoggerProvider"/>.
/// </summary>
/// <param name="Category">The logger category.</param>
/// <param name="Level">The entry's level.</param>
/// <param name="Message">The formatted message.</param>
internal sealed record CapturedLogEntry(string Category, LogLevel Level, string Message);
