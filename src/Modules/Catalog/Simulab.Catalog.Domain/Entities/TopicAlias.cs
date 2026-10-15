using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// Another name a <see cref="Topic"/> goes by (F-77, ADR-0001 #45): "Compreensão" for "Interpretação de texto".
/// It is only text. It carries no subject of its own: the subject is the topic's, so a move needs no update.
/// </summary>
public sealed class TopicAlias : TenantEntity, IAlias
{
    private TopicAlias()
    {
    }

    /// <summary>The topic it points to.</summary>
    public Guid TopicId { get; private set; }

    /// <summary>The text as typed.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Name"/>: what the unique index and the lookup read.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public static Result<TopicAlias> Create(Guid topicId, string? name)
    {
        var alias = new TopicAlias { TopicId = topicId };
        var applied = alias.Apply(name);

        return applied.IsFailure
            ? Result.Failure<TopicAlias>(applied.Error!)
            : Result.Success(alias);
    }

    /// <summary>Changes the typed form of the text, for a kept alias typed again with another case or accent.</summary>
    public Result Rename(string? name) => Apply(name);

    private Result Apply(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.TopicAliasTooShort, ErrorKind.Validation));
        }

        if (trimmed.Length > CatalogLimits.TopicNameMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.TopicAliasTooLong, ErrorKind.Validation));
        }

        Name = trimmed;
        NormalizedName = CatalogText.Normalize(trimmed);

        return Result.Success();
    }
}
