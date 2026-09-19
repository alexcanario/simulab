using System.Net.Mail;

namespace Simulab.Web.Components.Pages.Identity;

/// <summary>The comfort check of an email field before a call (rule: ui). The Api stays the authority.</summary>
public static class EmailRules
{
    /// <summary>The message to show under the field, or null when the address may be sent.</summary>
    public static string? Check(string? email, string requiredMessage, string invalidMessage)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return requiredMessage;
        }

        return MailAddress.TryCreate(email.Trim(), out var address) && address.Address == email.Trim() && address.Host.Contains('.', StringComparison.Ordinal)
            ? null
            : invalidMessage;
    }
}
