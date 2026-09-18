namespace Simulab.Email.Tests;

/// <summary>A page of captured messages.</summary>
/// <param name="Messages">The messages, newest first.</param>
/// <param name="Total">How many Mailpit holds.</param>
public sealed record MailpitMessagePage(IReadOnlyList<MailpitMessage> Messages, int Total);
