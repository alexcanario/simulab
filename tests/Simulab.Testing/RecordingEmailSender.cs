using System.Collections.Concurrent;
using Simulab.Email;

namespace Simulab.Testing;

/// <summary>Keeps every message instead of sending it, so a test can read what the app wrote.</summary>
public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyList<EmailMessage> Messages => [.. _messages];

    public EmailMessage? Last => _messages.LastOrDefault();

    public int Count => _messages.Count;

    public void Clear() => _messages.Clear();

    /// <summary>The next send fails the way an unreachable mail server does; the ones after it work again.</summary>
    public bool FailNext { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (FailNext)
        {
            FailNext = false;
            throw new InvalidOperationException("The mail server could not be reached.");
        }

        _messages.Enqueue(message);
        return Task.CompletedTask;
    }
}
