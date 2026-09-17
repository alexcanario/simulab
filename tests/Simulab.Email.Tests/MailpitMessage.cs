namespace Simulab.Email.Tests;

/// <summary>One captured message, as Mailpit's HTTP API returns it.</summary>
/// <param name="From">Sender.</param>
/// <param name="To">Recipients.</param>
/// <param name="Subject">Subject line.</param>
public sealed record MailpitMessage(MailpitAddress From, IReadOnlyList<MailpitAddress> To, string Subject);
