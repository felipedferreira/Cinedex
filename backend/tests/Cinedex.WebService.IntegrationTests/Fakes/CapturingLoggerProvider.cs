using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Cinedex.WebService.IntegrationTests.Fakes;

internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<CapturedLogEntry> _entries = new();

    public IReadOnlyList<CapturedLogEntry> Entries => this._entries.ToArray();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, this._entries);

    public void Clear()
    {
        while (this._entries.TryDequeue(out _))
        {
        }
    }

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(
        string category,
        ConcurrentQueue<CapturedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            IReadOnlyList<KeyValuePair<string, object?>> structuredState =
                state is IEnumerable<KeyValuePair<string, object?>> values
                    ? values.ToArray()
                    : [];

            entries.Enqueue(new CapturedLogEntry(
                category,
                logLevel,
                eventId,
                formatter(state, exception),
                structuredState));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
