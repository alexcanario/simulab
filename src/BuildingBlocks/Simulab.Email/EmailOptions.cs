using System.ComponentModel.DataAnnotations;

namespace Simulab.Email;

/// <summary>
/// Email settings, section <c>Email</c>. The SMTP keys are for the <see cref="EmailProvider.Smtp"/> provider and, in
/// development, point at the Mailpit container; the sender address and name serve both providers.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Which sender delivers the messages (F-66 BR3). SMTP when absent.</summary>
    public EmailProvider Provider { get; set; } = EmailProvider.Smtp;

    /// <summary>Settings of <see cref="EmailProvider.AzureCommunicationServices"/>.</summary>
    public AzureCommunicationServicesOptions AzureCommunicationServices { get; set; } = new();

    /// <summary>SMTP host name.</summary>
    [Required]
    public string Host { get; set; } = "localhost";

    /// <summary>SMTP port.</summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 1025;

    /// <summary>
    /// Address every message is sent from. No default: a cloud host that forgets it stops at start (F-66 BR4)
    /// instead of sending from a made-up address. Development sets it in <c>appsettings.Development.json</c>.
    /// </summary>
    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>Display name of the sender.</summary>
    [Required]
    public string FromName { get; set; } = "Simulab";

    /// <summary>User name, when the server requires authentication. Empty in development.</summary>
    public string? UserName { get; set; }

    /// <summary>Password, from user secrets or the vault. Never committed.</summary>
    public string? Password { get; set; }

    /// <summary>Use STARTTLS when the server offers it. Off for Mailpit.</summary>
    public bool UseStartTls { get; set; }
}
