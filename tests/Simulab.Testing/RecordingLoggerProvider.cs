using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Simulab.Testing;

/// <summary>Keeps every log entry written through it, from every category, so a test can read what the console would show.</summary>
public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<RecordedLogEntry> _entries = new();

    /// <summary>Every entry so far, in the order it was written.</summary>
    public IReadOnlyList<RecordedLogEntry> Entries => [.. _entries];

    public ILogger CreateLogger(string categoryName) => new RecordingLogger(categoryName, _entries);

    public void Dispose()
    {
    }

    private sealed class RecordingLogger(string category, ConcurrentQueue<RecordedLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            entries.Enqueue(new RecordedLogEntry(category, logLevel, eventId, formatter(state, exception)));
        }
    }
}
