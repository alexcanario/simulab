using Microsoft.Extensions.Localization;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// How a canonical subject or topic a notice subject covers reads on screen (F-75, BR13): the section's chips and the
/// dialog's picks share it. Names are content, shown as typed and never translated; only the marker and the form
/// around them are resources, so a language can change the "›".
/// </summary>
public static class NoticeSubjectCoverText
{
    /// <summary>
    /// The order on screen, whatever the Api sent: subject name (case and accents ignored), the whole subject before
    /// its topics, then topic name. A null topic name is a whole subject.
    /// </summary>
    public static IReadOnlyList<T> Sort<T>(IEnumerable<T> items, Func<T, string> subjectName, Func<T, string?> topicName)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(subjectName);
        ArgumentNullException.ThrowIfNull(topicName);

        return
        [
            .. items
                .OrderBy(item => AppSuggestField.Normalize(subjectName(item)), StringComparer.Ordinal)
                .ThenBy(item => topicName(item) is null ? 0 : 1)
                .ThenBy(item => AppSuggestField.Normalize(topicName(item)), StringComparer.Ordinal)
        ];
    }

    /// <summary>The chip's visible text: the subject's name, or "Subject › Topic" for a topic.</summary>
    public static string Visible(IStringLocalizer localizer, string subjectName, string? topicName)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return topicName is null ? subjectName : localizer["NoticeSubjects.Covers.Topic", subjectName, topicName].Value;
    }

    /// <summary>What a screen reader hears: "Mathematics, whole subject" or "Propositions, topic of Logic".</summary>
    public static string Spoken(IStringLocalizer localizer, string subjectName, string? topicName)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return topicName is null
            ? $"{subjectName}, {localizer["NoticeSubjects.Covers.WholeMarker"].Value}"
            : localizer["NoticeSubjects.Covers.Topic.Spoken", subjectName, topicName].Value;
    }
}
