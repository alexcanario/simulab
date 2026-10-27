using Microsoft.Extensions.Localization;
using Simulab.Catalog.Contracts;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// F-168: how the exam rules of an edition read, shared by the edition form's aside and the student's exam page so the
/// two never say a duration differently.
/// </summary>
public static class ExamRulesText
{
    /// <summary>"5 h", "4 h 30 min" or "45 min".</summary>
    public static string Duration(IStringLocalizer localizer, int minutes)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        var hours = minutes / 60;
        var rest = minutes % 60;

        return (hours, rest) switch
        {
            (0, _) => localizer["ExamRules.Duration.Minutes", rest],
            (_, 0) => localizer["ExamRules.Duration.Hours", hours],
            _ => localizer["ExamRules.Duration.HoursMinutes", hours, rest]
        };
    }

    /// <summary>
    /// UC4: the one line of a published edition's card, with only the rules it has — "5 h · 100 points, pass at 60 ·
    /// Essay 20 points, pass at 10" — or null when it has none.
    /// </summary>
    public static string? Line(IStringLocalizer localizer, PublishedExamEditionResponse edition)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentNullException.ThrowIfNull(edition);

        List<string> parts = [];
        if (edition.DurationMinutes is { } minutes)
        {
            parts.Add(Duration(localizer, minutes));
        }

        switch (edition.ObjectiveMaxScore, edition.ObjectiveCutOffScore)
        {
            case ({ } total, { } cutOff):
                parts.Add(localizer["CatalogExam.Rules.ObjectiveWithCutOff", ScoreText.Compact(total), ScoreText.Compact(cutOff)]);
                break;
            case ({ } total, null):
                parts.Add(localizer["CatalogExam.Rules.Objective", ScoreText.Compact(total)]);
                break;
            case (null, { } cutOff):
                parts.Add(localizer["CatalogExam.Rules.CutOffOnly", ScoreText.Compact(cutOff)]);
                break;
        }

        if (edition.EssayMaxScore is { } essayMax)
        {
            parts.Add(edition.EssayCutOffScore is { } essayCutOff
                ? localizer["CatalogExam.Rules.EssayWithCutOff", ScoreText.Compact(essayMax), ScoreText.Compact(essayCutOff)]
                : localizer["CatalogExam.Rules.Essay", ScoreText.Compact(essayMax)]);
        }

        return parts.Count == 0 ? null : string.Join(" · ", parts);
    }
}
