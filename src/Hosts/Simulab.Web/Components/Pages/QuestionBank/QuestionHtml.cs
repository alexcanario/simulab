using System.Net;
using System.Text.RegularExpressions;

namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>
/// What the question screens may know about the HTML an editor holds (F-95). It is comfort only: the Api sanitizes
/// every text and decides what is empty or too long, so these checks only spare the curator a round trip.
/// </summary>
public static partial class QuestionHtml
{
    [GeneratedRegex("<[^>]*>")]
    private static partial Regex TagPattern();

    /// <summary>The text of the HTML with its tags removed, entities decoded, trimmed (BR3: empty after tags counts as empty).</summary>
    public static string PlainText(string? html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var text = WebUtility.HtmlDecode(TagPattern().Replace(html, " "));

        return text.Replace(' ', ' ').Trim();
    }

    /// <summary>True for no HTML, for tags with nothing in them (<c>&lt;p&gt;&lt;br&gt;&lt;/p&gt;</c>) and for blanks.</summary>
    public static bool IsBlank(string? html) => PlainText(html).Length == 0;

    /// <summary>An optional text with nothing in it is sent as null, not as the editor's empty paragraph.</summary>
    public static string? NullIfBlank(string? html) => IsBlank(html) ? null : html;

    /// <summary>The first <paramref name="maxLength"/> characters of the plain text, with an ellipsis when it was cut.</summary>
    public static string Excerpt(string? text, int maxLength)
    {
        var value = text?.Trim() ?? string.Empty;

        return value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength).TrimEnd(), "…");
    }
}
