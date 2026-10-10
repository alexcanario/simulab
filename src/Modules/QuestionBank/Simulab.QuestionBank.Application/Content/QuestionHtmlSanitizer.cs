using Simulab.QuestionBank.Domain;

namespace Simulab.QuestionBank.Application.Content;

/// <summary>
/// The one place that decides what HTML a question may store (F-95, BR8 and BR8b). Every text field passes
/// through it on every write; the Web shows what comes back and never calls it. The allow-list is a single
/// list a test pins, so widening it is a visible change.
/// </summary>
public sealed class QuestionHtmlSanitizer
{
    /// <summary>The tags a stored text may hold (BR8).</summary>
    public static IReadOnlyList<string> AllowedTags => throw new NotImplementedException();

    /// <summary>The sanitized HTML and its plain text; both empty when <paramref name="html"/> is null or only empty tags.</summary>
    public SanitizedHtml Sanitize(string? html) => throw new NotImplementedException();
}
