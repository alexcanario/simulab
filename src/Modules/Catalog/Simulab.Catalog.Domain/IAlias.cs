using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain;

/// <summary>
/// What the diff of a requested set of other names needs from an alias row (F-77): its text and the way to change
/// the typed form. Implemented by the subject alias and the topic alias, which share the rule and nothing else.
/// </summary>
public interface IAlias
{
    /// <summary>The text as typed.</summary>
    string Name { get; }

    /// <summary>The comparable form of <see cref="Name"/>.</summary>
    string NormalizedName { get; }

    /// <summary>Changes the typed form of the text; the normalized form does not change.</summary>
    Result Rename(string? name);
}
