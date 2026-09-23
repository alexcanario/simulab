using Microsoft.Extensions.Logging;

namespace Simulab.Testing;

/// <summary>One log entry as <see cref="RecordingLoggerProvider"/> kept it.</summary>
public sealed record RecordedLogEntry(string Category, LogLevel Level, EventId EventId, string Message)
{
    public override string ToString() => $"{Level} {Category}[{EventId.Id}] {Message}";
}
