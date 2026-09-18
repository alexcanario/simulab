using System.ComponentModel.DataAnnotations;

namespace Simulab.Email;

/// <summary>SMTP settings, section <c>Email</c>. In development they point at the Mailpit container.</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>SMTP host name.</summary>
    [Required]
    public string Host { get; set; } = "localhost";

    /// <summary>SMTP port.</summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 1025;

    /// <summary>Address every message is sent from.</summary>
    [Required]
    [EmailAddress]
    public string FromAddress { get; set; } = "no-reply@simulab.app";

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
