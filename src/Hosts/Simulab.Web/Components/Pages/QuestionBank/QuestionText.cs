using System.Globalization;
using Microsoft.Extensions.Localization;
using Simulab.QuestionBank.Contracts;
using Simulab.Web.Components.Ui;
using Simulab.Web.Localization;
using Simulab.Web.Resources;

namespace Simulab.Web.Components.Pages.QuestionBank;

/// <summary>The words the question screens use for a status, a letter and a language (F-95).</summary>
public static class QuestionText
{
    /// <summary>The longest excerpt of a statement that is part of a button's accessible name.</summary>
    public const int RowNameLength = 60;

    public static string StatusName(IStringLocalizer<SharedResources> l, QuestionStatus status)
    {
        ArgumentNullException.ThrowIfNull(l);

        return status == QuestionStatus.Published ? l["Questions.Status.Published"] : l["Questions.Status.Draft"];
    }

    /// <summary>The name of a question type in the curator's language (F-97, BR8).</summary>
    public static string TypeName(IStringLocalizer<SharedResources> l, QuestionType type)
    {
        ArgumentNullException.ThrowIfNull(l);

        return type == QuestionType.TrueFalse ? l["Questions.Type.TrueFalse"] : l["Questions.Type.SingleChoice"];
    }

    /// <summary>A draft is a plain label; a published question is the healthy state.</summary>
    public static AppStatusTone StatusTone(QuestionStatus status) =>
        status == QuestionStatus.Published ? AppStatusTone.Success : AppStatusTone.Neutral;

    /// <summary>
    /// The source line of a question from a past edition: organizer acronym, exam and year (F-96, BR7), or null
    /// for an authored question and for an origin whose edition no longer resolves (BR11). The position and the
    /// notice reference are not part of it.
    /// </summary>
    public static string? SourceLine(QuestionOriginResponse? origin)
    {
        return origin is null ? null : SourceLine(origin.OrganizerAcronym, origin.ExamName, origin.NoticeYear);
    }

    /// <summary>The same line from its parts, for the edition lookup; null when there is no exam name.</summary>
    public static string? SourceLine(string? organizerAcronym, string? examName, int? noticeYear)
    {
        if (string.IsNullOrWhiteSpace(examName))
        {
            return null;
        }

        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(organizerAcronym))
        {
            parts.Add(organizerAcronym);
        }

        parts.Add(examName);
        if (noticeYear is { } year)
        {
            parts.Add(year.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join(" · ", parts);
    }

    /// <summary>The letter of an option, A to E, from its position (BR5).</summary>
    public static string Letter(int position) => ((char)('A' + position)).ToString();

    /// <summary>A language in its own name, with the tag the text is marked with; the same list as the exam form.</summary>
    public static string LanguageName(string code) =>
        SupportedCultures.All.FirstOrDefault(culture => string.Equals(culture.Name, code, StringComparison.OrdinalIgnoreCase)) is { } culture
            ? SupportedCultures.NativeName(culture)
            : code;

    /// <summary>The language the form starts with: the curator's UI language when the app supports it (BR7).</summary>
    public static string DefaultContentLanguage() =>
        SupportedCultures.All.FirstOrDefault(culture =>
            string.Equals(culture.Name, CultureInfo.CurrentUICulture.Name, StringComparison.OrdinalIgnoreCase))?.Name
        ?? SupportedCultures.Default;
}
