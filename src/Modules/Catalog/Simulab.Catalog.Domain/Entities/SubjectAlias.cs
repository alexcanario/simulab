using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// Another name a <see cref="Subject"/> goes by (F-77, ADR-0001 #45): "Português" for "Língua Portuguesa". It is
/// only text, with no organizer and no language. Whether the text is free across the vocabulary needs the
/// tables, so that rule lives in the handler.
/// </summary>
public sealed class SubjectAlias : TenantEntity, IAlias
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

    public static Result<SubjectAlias> Create(Guid subjectId, string? name)
    {
        var alias = new SubjectAlias { SubjectId = subjectId };
        var applied = alias.Apply(name);

        return applied.IsFailure
            ? Result.Failure<SubjectAlias>(applied.Error!)
            : Result.Success(alias);
    }

    /// <summary>Changes the typed form of the text, for a kept alias typed again with another case or accent.</summary>
    public Result Rename(string? name) => Apply(name);

    private Result Apply(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.SubjectAliasTooShort, ErrorKind.Validation));
        }

        if (trimmed.Length > CatalogLimits.SubjectNameMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.SubjectAliasTooLong, ErrorKind.Validation));
        }

        Name = trimmed;
        NormalizedName = CatalogText.Normalize(trimmed);

        return Result.Success();
    }
}
