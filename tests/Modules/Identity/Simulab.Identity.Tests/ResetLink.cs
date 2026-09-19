using System.Text.RegularExpressions;

namespace Simulab.Identity.Tests;

/// <summary>Reads the raw token out of a password reset email, the way a person clicking the link would.</summary>
public static partial class ResetLink
{
    public static string TokenOf(string htmlBody)
    {
        var match = TokenPattern().Match(htmlBody);
        if (!match.Success)
        {
            throw new InvalidOperationException("The email carries no reset link.");
        }

        return Uri.UnescapeDataString(match.Groups["token"].Value);
    }

    [GeneratedRegex(@"reset-password\?token=(?<token>[A-Za-z0-9_\-%]+)")]
    private static partial Regex TokenPattern();
}
