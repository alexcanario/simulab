using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// A topic under a <see cref="Subject"/> (F-79, BR6): "Crase" under "Portuguese". Its name is unique inside
/// its subject, which the table and the handler check; the entity only owns the shape of the name.
/// </summary>
public sealed class Topic : TenantEntity
{
    private Topic()
    {
    }

    /// <summary>The subject it sits under; an update may move it (BR10).</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>The name as typed, one name and not translated.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Name"/>: what the unique index reads.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    public static Result<Topic> Create(Guid subjectId, string? name)
    {
        var topic = new Topic();
        var applied = topic.Apply(subjectId, name);

        return applied.IsFailure
            ? Result.Failure<Topic>(applied.Error!)
            : Result.Success(topic);
    }

    public Result Update(Guid subjectId, string? name) => Apply(subjectId, name);

    private Result Apply(Guid subjectId, string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.TopicNameRequired, ErrorKind.Validation));
        }

        if (trimmed.Length > CatalogLimits.TopicNameMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.TopicNameTooLong, ErrorKind.Validation));
        }

        SubjectId = subjectId;
        Name = trimmed;
        NormalizedName = CatalogText.Normalize(trimmed);

        return Result.Success();
    }
}
