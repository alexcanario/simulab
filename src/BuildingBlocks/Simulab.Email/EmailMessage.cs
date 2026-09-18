namespace Simulab.Email;

/// <summary>One message to send. The caller renders the body; templates and languages belong to the feature (F-4).</summary>
/// <param name="To">Recipient address.</param>
/// <param name="Subject">Subject line, already in the recipient's language.</param>
/// <param name="HtmlBody">HTML body.</param>
/// <param name="TextBody">Plain-text alternative; optional.</param>
public sealed record EmailMessage(string To, string Subject, string HtmlBody, string? TextBody = null);
