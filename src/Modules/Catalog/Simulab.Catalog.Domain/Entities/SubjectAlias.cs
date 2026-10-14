using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// Another name a <see cref="Subject"/> goes by (F-77, ADR-0001 #45): "Português" for "Língua Portuguesa". It is
/// only text, with no organizer and no language. Whether the text is free across the vocabulary needs the
/// tables, so that rule lives in the handler.
/// </summary>
public sealed class SubjectAlias : TenantEntity
{
    private SubjectAlias()
    {
    }

    /// <summary>The subject it points to.</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>The text as typed.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Name"/>: what the unique index and the lookup read.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public static Result<SubjectAlias> Create(Guid subjectId, string? name) => throw new NotImplementedException();

    /// <summary>Changes the typed form of the text, for a kept alias typed again with another case or accent.</summary>
    public Result Rename(string? name) => throw new NotImplementedException();
}
