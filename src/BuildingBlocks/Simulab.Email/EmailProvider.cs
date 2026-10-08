namespace Simulab.Email;

/// <summary>Which sender delivers the messages. Chosen by configuration (<c>Email:Provider</c>), never by environment name.</summary>
public enum EmailProvider
{
    /// <summary>SMTP through MailKit. The default; in development it points at Mailpit.</summary>
    Smtp = 0,

    /// <summary>Azure Communication Services Email over its HTTP API, signed in with the host's managed identity.</summary>
    AzureCommunicationServices = 1,
}
