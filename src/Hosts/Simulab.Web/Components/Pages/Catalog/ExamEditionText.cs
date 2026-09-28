using Microsoft.Extensions.Localization;
using Simulab.Catalog.Contracts;
using Simulab.Web.Components.Ui;
using Simulab.Web.Resources;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// The reader's words for an exam edition (F-35): how a row names it, and what its state says and shows. The
/// enum name never reaches a screen: it is a key, and the three languages give it their own text (rule: i18n).
/// </summary>
public static class ExamEditionText
{
    /// <summary>The visible separator between the parts of a row's label; hidden from a screen reader.</summary>
    public const string VisibleSeparator = " · ";

    /// <summary>The separator of the name a screen reader hears: commas read as pauses.</summary>
    public const string SpokenSeparator = ", ";

    public static string StatusName(IStringLocalizer<SharedResources> l, ExamEditionStatus status)
    {
        ArgumentNullException.ThrowIfNull(l);

        return l[$"ExamEditions.Status.{status}"];
    }

    /// <summary>A published edition is the one state worth a colour; a draft is only a draft.</summary>
    public static AppStatusTone StatusTone(ExamEditionStatus status) =>
        status == ExamEditionStatus.Published ? AppStatusTone.Success : AppStatusTone.Neutral;

    /// <summary>
    /// The year, the position when there is one, and the board's acronym: what tells two editions of the
    /// same year apart (owner, screen question 14).
    /// </summary>
    public static string Label(ExamEditionResponse edition, string separator)
    {
        ArgumentNullException.ThrowIfNull(edition);

        return string.Join(
            separator,
            new[] { edition.NoticeYear.ToString(System.Globalization.CultureInfo.InvariantCulture), edition.Position, edition.OrganizerAcronym }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    /// <summary>The year and the position, without the board: the saved values the edition page's breadcrumb shows.</summary>
    public static string YearAndPosition(ExamEditionResponse edition)
    {
        ArgumentNullException.ThrowIfNull(edition);

        return string.Join(
            VisibleSeparator,
            new[] { edition.NoticeYear.ToString(System.Globalization.CultureInfo.InvariantCulture), edition.Position }
                .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
