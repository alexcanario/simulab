using System.Globalization;
using Simulab.ExamEngine.Contracts;

namespace Simulab.Web.Components.Pages.Attempts;

/// <summary>The words the simulator's screens use for the edition an attempt belongs to (F-169).</summary>
internal static class AttemptText
{
    /// <summary>
    /// The exam, its notice year, its position when there is one, and the board's acronym: what the session's bar and the
    /// submitted page call the edition. Null when the edition no longer exists.
    /// </summary>
    internal static string? EditionLabel(AttemptEditionResponse? edition)
    {
        if (edition is null)
        {
            return null;
        }

        var exam = string.Join(' ', edition.ExamName, edition.NoticeYear.ToString(CultureInfo.InvariantCulture));
        return string.Join(
            " · ",
            new[] { exam, edition.Position, edition.OrganizerAcronym }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
