using System.Text.RegularExpressions;

namespace Simulab.QuestionBank.Domain;

/// <summary>
/// The one rule of the module for typed answers (F-107, BR6; F-108 reuses it): trim, collapse every run of white space
/// to one space, and compare without regard to case (invariant culture). Accents and every other character count, so
/// "Brasilia" is not "Brasília". This is not <c>ComparableText</c>, which strips accents for search.
/// </summary>
public static partial class AcceptedAnswer
{
    /// <summary>The form two answers are compared in.</summary>
    public static string Normalize(string? answer) =>
        string.IsNullOrWhiteSpace(answer) ? string.Empty : WhiteSpace().Replace(answer.Trim(), " ").ToUpperInvariant();

    /// <summary>True when <paramref name="typed"/> matches <paramref name="accepted"/> under the rule.</summary>
    public static bool Matches(string? typed, string? accepted) =>
        string.Equals(Normalize(typed), Normalize(accepted), StringComparison.Ordinal);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();
}
