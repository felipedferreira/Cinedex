using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace FoundryOceanus.Observability.OpenTelemetry.Tests.Fakes;

/// <summary>
/// An <see cref="ILoggerProvider"/> that keeps every entry in memory, so a test can assert on what the
/// library logged without a backend.
/// </summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLogEntry> entries = new();

    /// <summary>
    /// Gets a snapshot of every entry logged so far, in order.
    /// </summary>
    public IReadOnlyList<CapturedLogEntry> Entries => [.. entries];

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, entries);

    /// <inheritdoc/>
    public void Dispose()
    {
    }
}
