using System.Collections.Concurrent;
using Simulab.Email;

namespace Simulab.Identity.Tests;

/// <summary>Keeps every message instead of sending it, so a test can read what the app wrote.</summary>
public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyList<EmailMessage> Messages => [.. _messages];

    public EmailMessage? Last => _messages.LastOrDefault();

    public int Count => _messages.Count;

    public void Clear() => _messages.Clear();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
