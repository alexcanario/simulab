using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Simulab.QuestionBank.Contracts;

namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>
/// The blank markers of a statement as the screens read them (F-107, BR3): <see cref="BlankMarker.Pattern"/> applied to
/// the text between tags only, so a marker split by formatting or inside an attribute is plain text, as the Api reads it.
/// The form uses it to draw one row per marker (comfort: the Api decides); the preview to draw the boxes.
/// </summary>
public static partial class StatementBlanks
{
    // A tag, its quoted attribute values skipped: the editor's HTML does not encode '>' inside an attribute (an alt
    // such as "a > [[3]]"), and the text there is never a marker (review #1).
    [GeneratedRegex("""<(?:[^>"']|"[^"]*"|'[^']*')*>""")]
    private static partial Regex TagPattern();

    [GeneratedRegex(BlankMarker.Pattern)]
    private static partial Regex MarkerPattern();

    /// <summary>The marker numbers of <paramref name="html"/>, in order of appearance and with repeats.</summary>
    public static IReadOnlyList<int> Markers(string? html)
    {
        List<int> numbers = [];
        Rewrite(html, number =>
        {
            numbers.Add(number);
            return null;
        });

        return numbers;
    }

    /// <summary>
    /// <paramref name="html"/> with each marker replaced by what <paramref name="replacement"/> answers for its number;
    /// the tags and every other text are kept as they are. The replacement must be safe HTML.
    /// </summary>
    public static string Replace(string html, Func<int, string> replacement)
    {
        ArgumentNullException.ThrowIfNull(replacement);

        return Rewrite(html, replacement);
    }

    // One pass over the text segments: a null replacement keeps the marker as it was.
    private static string Rewrite(string? html, Func<int, string?> replacement)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var result = new StringBuilder(html.Length);
        var start = 0;
        foreach (Match tag in TagPattern().Matches(html))
        {
            result.Append(RewriteText(html[start..tag.Index], replacement)).Append(tag.Value);
            start = tag.Index + tag.Length;
        }

        return result.Append(RewriteText(html[start..], replacement)).ToString();
    }

    private static string RewriteText(string text, Func<int, string?> replacement) =>
        MarkerPattern().Replace(text, match =>
            replacement(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture)) ?? match.Value);
}
