using System.Net;

namespace Simulab.Identity.Infrastructure.Email;

/// <summary>
/// The pieces of markup every Identity email shares. B-9: the button colour was written once in each mailer
/// and stayed on the old primary blue when B-8 changed the app's; it lives here now, in one place.
/// </summary>
public static class EmailHtml
{
    /// <summary>The app's light primary since B-8. White on it reads at 5.36:1 (B-9 AC3).</summary>
    public const string ButtonColour = "#216DB5";

    /// <summary>The call to action of an email: one link painted as a button.</summary>
    public static string Button(string link, string text) =>
        $"""
        <p>
          <a href="{WebUtility.HtmlEncode(link)}" style="display: inline-block; background: {ButtonColour}; color: #FFFFFF; padding: 12px 20px; border-radius: 8px; text-decoration: none; font-weight: 700;">
            {WebUtility.HtmlEncode(text)}
          </a>
        </p>
        """;
}
