using System.Net;

namespace Simulab.Identity.Infrastructure.Email;

/// <summary>
/// The pieces of markup every Identity email shares. B-9: the button colour was written once in each mailer
/// and stayed on the old primary blue when B-8 changed the app's; it lives here now, in one place.
/// F-125 (BR12): the page itself lives here too, in the Control Alt Systems identity: ink text, the Bricolage Grotesque
/// stack with its fallback (email clients load no web font), and the brand as text at the top, never an image.
/// </summary>
public static class EmailHtml
{
    /// <summary>The app's light primary since F-148, the brand navy. White on it reads at 12.59:1.</summary>
    public const string ButtonColour = "#0F3266";

    /// <summary>Ink: the text of the email (17.35:1 on white).</summary>
    public const string TextColour = "#0B1B2E";

    /// <summary>Ink-muted: the small print under the button (6.06:1 on white).</summary>
    public const string MutedColour = "#52647A";

    public const string FontStack = "'Bricolage Grotesque', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif";

    /// <summary>A whole email: the brand, the heading, then the body the mailer wrote (already encoded).</summary>
    public static string Page(string heading, string innerHtml) =>
        $"""
        <!doctype html>
        <html>
          <body style="font-family: {FontStack}; color: {TextColour}; line-height: 1.6;">
            <p style="margin: 0; font-size: 18px; font-weight: 800;">Simulab</p>
            <p style="margin: 0 0 24px; font-size: 12px; color: {MutedColour};">Control Alt Systems</p>
            <h1 style="font-size: 20px; font-weight: 800;">{WebUtility.HtmlEncode(heading)}</h1>
            {innerHtml}
          </body>
        </html>
        """;

    /// <summary>A line of small print in the muted ink.</summary>
    public static string SmallPrint(string encodedHtml) =>
        $"""<p style="color: {MutedColour}; font-size: 13px;">{encodedHtml}</p>""";

    /// <summary>The call to action of an email: one link painted as a button.</summary>
    public static string Button(string link, string text) =>
        $"""
        <p>
          <a href="{WebUtility.HtmlEncode(link)}" style="display: inline-block; background: {ButtonColour}; color: #FFFFFF; padding: 12px 20px; border-radius: 12px; text-decoration: none; font-weight: 800;">
            {WebUtility.HtmlEncode(text)}
          </a>
        </p>
        """;
}
