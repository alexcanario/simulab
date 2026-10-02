namespace Simulab.Catalog.Contracts;

/// <summary>One Brazilian state (or the Federal District): the acronym an exam stores and the name a reader sees (F-42).</summary>
/// <param name="Acronym">Two upper-case letters, what <c>exams.scope_detail</c> holds for a State exam.</param>
/// <param name="Name">The name as written, with its accents.</param>
public sealed record BrazilianState(string Acronym, string Name)
{
    /// <summary>How a state reads wherever it is shown: <c>São Paulo (SP)</c> (F-42 BR4).</summary>
    public string DisplayName => $"{Name} ({Acronym})";
}
