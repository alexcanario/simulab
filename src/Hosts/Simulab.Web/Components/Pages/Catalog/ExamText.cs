using System.Globalization;
using Microsoft.Extensions.Localization;
using Simulab.Catalog.Contracts;
using Simulab.Web.Localization;
using Simulab.Web.Resources;

namespace Simulab.Web.Components.Pages.Catalog;

/// <summary>
/// The reader's words for an exam's assessment type and scope (F-34, BR6 and BR7). The enum name never
/// reaches a screen: it is a key, and the three languages give it their own text (rule: i18n). The orders
/// are what the API's `assessmentTypeOrder` and `scopeOrder` expect, first to last (B-15): a column whose
/// label is translated is sorted in the reader's culture, which only the Web knows.
/// </summary>
public static class ExamText
{
    public static string AssessmentTypeName(IStringLocalizer<SharedResources> l, AssessmentType assessmentType)
    {
        ArgumentNullException.ThrowIfNull(l);

        return l[$"Exams.AssessmentType.{assessmentType}"];
    }

    public static string ScopeName(IStringLocalizer<SharedResources> l, ExamScope scope)
    {
        ArgumentNullException.ThrowIfNull(l);

        return l[$"Exams.Scope.{scope}"];
    }

    /// <summary>
    /// F-42 BR4: the place an exam applies to, as a reader sees it. A State exam stores an acronym and reads
    /// <c>São Paulo (SP)</c>; a value the list does not know (BR6) and every Municipal detail read as stored.
    /// </summary>
    public static string? ScopeDetailText(ExamScope scope, string? scopeDetail) =>
        scope == ExamScope.State ? BrazilianStates.Display(scopeDetail) : scopeDetail;

    /// <summary>
    /// F-36: an exam's content language in its own name ("Português (Brasil)"), the way the exam form's picker shows it;
    /// the raw tag when it is not one of the UI languages.
    /// </summary>
    public static string ContentLanguageName(string contentLanguage)
    {
        var culture = SupportedCultures.All.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, contentLanguage, StringComparison.OrdinalIgnoreCase));

        return culture is null ? contentLanguage : SupportedCultures.NativeName(culture);
    }

    public static IReadOnlyList<AssessmentType> AssessmentTypeOrder(IStringLocalizer<SharedResources> l) =>
        Ordered(Enum.GetValues<AssessmentType>(), value => AssessmentTypeName(l, value));

    public static IReadOnlyList<ExamScope> ScopeOrder(IStringLocalizer<SharedResources> l) =>
        Ordered(Enum.GetValues<ExamScope>(), value => ScopeName(l, value));

    private static IReadOnlyList<TEnum> Ordered<TEnum>(TEnum[] values, Func<TEnum, string> name)
    {
        var culture = CultureInfo.CurrentUICulture;

        return [.. values.OrderBy(name, StringComparer.Create(culture, ignoreCase: false))];
    }
}
