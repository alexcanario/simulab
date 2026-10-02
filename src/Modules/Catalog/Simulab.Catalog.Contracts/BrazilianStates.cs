using Simulab.SharedKernel.Text;

namespace Simulab.Catalog.Contracts;

/// <summary>
/// The 26 Brazilian states and the Federal District, in the order the form offers them (F-42 BR1). A static
/// list and not a table: it changes by constitutional amendment, not by an editor. The domain and the Web
/// share it, so the name a reader sees and the acronym an exam stores cannot drift apart. It is the same list
/// whatever the exam's content language (BR7); Portugal's districts are epic 704's.
/// </summary>
public static class BrazilianStates
{
    public static IReadOnlyList<BrazilianState> All { get; } =
    [
        new("AC", "Acre"),
        new("AL", "Alagoas"),
        new("AP", "Amapá"),
        new("AM", "Amazonas"),
        new("BA", "Bahia"),
        new("CE", "Ceará"),
        new("DF", "Distrito Federal"),
        new("ES", "Espírito Santo"),
        new("GO", "Goiás"),
        new("MA", "Maranhão"),
        new("MT", "Mato Grosso"),
        new("MS", "Mato Grosso do Sul"),
        new("MG", "Minas Gerais"),
        new("PA", "Pará"),
        new("PB", "Paraíba"),
        new("PR", "Paraná"),
        new("PE", "Pernambuco"),
        new("PI", "Piauí"),
        new("RJ", "Rio de Janeiro"),
        new("RN", "Rio Grande do Norte"),
        new("RS", "Rio Grande do Sul"),
        new("RO", "Rondônia"),
        new("RR", "Roraima"),
        new("SC", "Santa Catarina"),
        new("SP", "São Paulo"),
        new("SE", "Sergipe"),
        new("TO", "Tocantins")
    ];

    /// <summary>The state whose acronym is <paramref name="acronym"/>, ignoring case and surrounding spaces (BR2); null when none is.</summary>
    public static BrazilianState? FindByAcronym(string? acronym)
    {
        var trimmed = acronym?.Trim();

        return string.IsNullOrEmpty(trimmed)
            ? null
            : All.FirstOrDefault(state => string.Equals(state.Acronym, trimmed, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// What an exam's scope detail reads as: <c>São Paulo (SP)</c> for a stored acronym, the stored text when the
    /// list does not know it (BR4, BR6), null when there is none.
    /// </summary>
    public static string? Display(string? scopeDetail) =>
        FindByAcronym(scopeDetail)?.DisplayName ?? scopeDetail;

    /// <summary>
    /// The states whose name, acronym or displayed form (<c>São Paulo (SP)</c>) contains <paramref name="term"/>,
    /// ignoring case and accents, in list order; every state when the term is blank (AC1, AC2). The displayed form
    /// is matched too because a picker that already holds a state searches with that very text when it is focused
    /// again, and must still offer the state it holds.
    /// </summary>
    public static IReadOnlyList<BrazilianState> Search(string? term)
    {
        var wanted = ComparableText.Normalize(term);

        return wanted.Length == 0
            ? All
            : [.. All.Where(state => ComparableText.Normalize(state.DisplayName).Contains(wanted, StringComparison.Ordinal)
                || ComparableText.Normalize(state.Acronym).Contains(wanted, StringComparison.Ordinal))];
    }

    /// <summary>The comparable form of a state for the student search: <c>SAO PAULO SP</c> (F-42, F-36 BR4).</summary>
    public static string SearchText(BrazilianState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        return $"{ComparableText.Normalize(state.Name)} {state.Acronym}";
    }
}
