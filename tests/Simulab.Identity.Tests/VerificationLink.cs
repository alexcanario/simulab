using System.Text.RegularExpressions;

namespace Simulab.Identity.Tests;

/// <summary>Reads the raw token out of a verification email, the way a person clicking the link would.</summary>
public static partial class VerificationLink
{
    public static string TokenOf(string htmlBody)
    {
        var match = TokenPattern().Match(htmlBody);
        if (!match.Success)
        {
            throw new InvalidOperationException("The email carries no verification link.");
        }

        return Uri.UnescapeDataString(match.Groups["token"].Value);
    }

    [GeneratedRegex(@"verify-email\?token=(?<token>[A-Za-z0-9_\-%]+)")]
    private static partial Regex TokenPattern();
}
