using Microsoft.Extensions.Logging;

namespace Simulab.Jobs.Tests;

/// <summary>
/// Keeps what the code logged, so F-27 AC6 can say "one line with the count, and nothing when there was
/// nothing to remove" instead of trusting that the call was made.
/// </summary>
public sealed class RecordingLoggerProvider : ILoggerProvider
{
    private readonly List<(LogLevel Level, string Message)> _entries = [];

    public IReadOnlyList<(LogLevel Level, string Message)> Entries
    {
        get
        {
            lock (_entries)
            {
                return [.. _entries];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new Recorder(this);

    /// <summary>A typed logger for a class built by hand in a test, recording into the same list.</summary>
    public ILogger<T> CreateLogger<T>() => new Recorder<T>(this);

    public void Dispose()
    {
    }

    private void Add(LogLevel level, string message)
    {
        lock (_entries)
        {
            _entries.Add((level, message));
        }
    }

    private class Recorder(RecordingLoggerProvider provider) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            provider.Add(logLevel, formatter(state, exception));
    }

    private sealed class Recorder<T>(RecordingLoggerProvider provider) : Recorder(provider), ILogger<T>;
}
