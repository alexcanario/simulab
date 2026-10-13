using System.Globalization;
using Microsoft.Extensions.Localization;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>One heading and the consecutive rows under it; a null label is the rows with no group.</summary>
public sealed record NoticeSubjectGroup(string? Label, IReadOnlyList<NoticeSubjectResponse> Rows);

/// <summary>The stated numbers of a list of notice subjects: how many, their sum and how many state none.</summary>
public sealed record NoticeSubjectTotals(int StatedCount, long StatedSum, int UnstatedCount);

/// <summary>
/// How a list of notice subjects reads once it is on screen (F-74 BR6 and BR8), shared by the back office section and
/// the student's syllabus (F-76) so the two never disagree on a heading or on a sum.
/// </summary>
public static class NoticeSubjectGroups
{
    /// <summary>
    /// The Api sends the rows in display order and the rows of one group are consecutive: one group per run of rows
    /// with the same group, case and accents ignored, using the first row's text (F-74 BR6).
    /// </summary>
    public static IReadOnlyList<NoticeSubjectGroup> Group(IReadOnlyList<NoticeSubjectResponse> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var groups = new List<(string Key, string? Label, List<NoticeSubjectResponse> Rows)>();
        foreach (var row in rows)
        {
            var key = AppSuggestField.Normalize(row.Group);
            if (groups.Count == 0 || groups[^1].Key != key)
            {
                groups.Add((key, string.IsNullOrWhiteSpace(row.Group) ? null : row.Group.Trim(), []));
            }

            groups[^1].Rows.Add(row);
        }

        return [.. groups.Select(group => new NoticeSubjectGroup(group.Label, group.Rows))];
    }

    /// <summary>No heading is drawn when no row has a group: "No group" over the whole list says nothing.</summary>
    public static bool ShowHeadings(IReadOnlyList<NoticeSubjectGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(groups);

        return groups.Any(group => group.Label is not null);
    }

    /// <summary>F-74 BR8: the sum of the stated numbers; rows with no number are left out and counted apart.</summary>
    public static NoticeSubjectTotals Totals(IReadOnlyList<NoticeSubjectResponse> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var stated = rows.Where(row => row.QuestionCount is not null).ToList();

        return new NoticeSubjectTotals(stated.Count, stated.Sum(row => (long)row.QuestionCount!.Value), rows.Count - stated.Count);
    }

    /// <summary>"Stated in the notice: 15 questions in total".</summary>
    public static string TotalText(IStringLocalizer localizer, NoticeSubjectTotals totals)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(totals);

        return totals.StatedSum == 1
            ? localizer["NoticeSubjects.Total.One"]
            : localizer["NoticeSubjects.Total.Many", totals.StatedSum.ToString("N0", CultureInfo.CurrentCulture)];
    }

    /// <summary>"2 subjects have no number and are left out of the sum."</summary>
    public static string UnstatedText(IStringLocalizer localizer, NoticeSubjectTotals totals)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(totals);

        return totals.UnstatedCount == 1
            ? localizer["NoticeSubjects.Total.Unstated.One"]
            : localizer["NoticeSubjects.Total.Unstated.Many", totals.UnstatedCount.ToString("N0", CultureInfo.CurrentCulture)];
    }
}
