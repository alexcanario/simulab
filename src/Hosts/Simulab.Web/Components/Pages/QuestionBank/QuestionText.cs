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

        // Each status is named here; the last arm is the draft's, so a status added later is mapped in this switch
        // (its test reads the enum values) and not left to read as "Draft".
        return status switch
        {
            QuestionStatus.Published => l["Questions.Status.Published"],
            QuestionStatus.Annulled => l["Questions.Status.Annulled"],
            QuestionStatus.InReview => l["Questions.Status.InReview"],
            _ => l["Questions.Status.Draft"]
        };
    }

    /// <summary>What happened in one entry of the review history, in the curator's language (F-105).</summary>
    public static string ReviewKindName(IStringLocalizer<SharedResources> l, QuestionReviewKind kind)
    {
        ArgumentNullException.ThrowIfNull(l);

        return kind switch
        {
            QuestionReviewKind.Withdrawn => l["Questions.ReviewHistory.Kind.Withdrawn"],
            QuestionReviewKind.Approved => l["Questions.ReviewHistory.Kind.Approved"],
            QuestionReviewKind.Rejected => l["Questions.ReviewHistory.Kind.Rejected"],
            _ => l["Questions.ReviewHistory.Kind.Submitted"]
        };
    }

    /// <summary>A send is information, a withdrawal a plain label, an approval the healthy state, a rejection a refusal (F-105).</summary>
    public static AppStatusTone ReviewKindTone(QuestionReviewKind kind) =>
        kind switch
        {
            QuestionReviewKind.Withdrawn => AppStatusTone.Neutral,
            QuestionReviewKind.Approved => AppStatusTone.Success,
            QuestionReviewKind.Rejected => AppStatusTone.Error,
            _ => AppStatusTone.Info
        };

    /// <summary>The name of a question type in the curator's language (F-97, BR8).</summary>
    public static string TypeName(IStringLocalizer<SharedResources> l, QuestionType type)
    {
        ArgumentNullException.ThrowIfNull(l);

        return type switch
        {
            QuestionType.TrueFalse => l["Questions.Type.TrueFalse"],
            QuestionType.MultipleAnswer => l["Questions.Type.MultipleAnswer"],
            QuestionType.Matching => l["Questions.Type.Matching"],
            _ => l["Questions.Type.SingleChoice"]
        };
    }

    /// <summary>A draft is a plain label; a published question is the healthy state; an annulled one is cancelled.</summary>
    public static AppStatusTone StatusTone(QuestionStatus status) =>
        status switch
        {
            QuestionStatus.Published => AppStatusTone.Success,
            QuestionStatus.Annulled => AppStatusTone.Error,
            QuestionStatus.InReview => AppStatusTone.Warning,
            _ => AppStatusTone.Neutral
        };

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

    /// <summary>
    /// The line the reader picks in an edition lookup: the source line, with the position under it when there is
    /// one so look-alikes can be told apart (F-96; the form and the list's edition filter share it, F-100).
    /// </summary>
    public static AppLookupOption EditionOption(ExamEditionLookupResponse edition)
    {
        ArgumentNullException.ThrowIfNull(edition);

        return new AppLookupOption(
            edition.Id,
            SourceLine(edition.OrganizerAcronym, edition.ExamName, edition.NoticeYear) ?? edition.ExamName,
            edition.Position);
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
