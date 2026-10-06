using Simulab.Catalog.Contracts;
using Simulab.SharedKernel.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Domain.Entities;

/// <summary>
/// A subject of the canonical taxonomy (F-79, ADR-0001 #43): "Constitutional Law", "Portuguese". Global
/// data (<see cref="TenantEntity.TenantId"/> null) whose rules answer with a <see cref="Result"/> and never
/// throw. Uniqueness and the topics that hold a delete back need the table, so they live in the handlers.
/// </summary>
public sealed class Subject : TenantEntity
{
    private Subject()
    {
    }

    /// <summary>The name as typed: content, so it is one name and not translated (BR3).</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>The comparable form of <see cref="Name"/>: what the unique index and the search read.</summary>
    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>The area it belongs to, null when it has none (BR4).</summary>
    public Guid? AreaId { get; private set; }

    public static Result<Subject> Create(string? name, Guid? areaId)
    {
        var subject = new Subject();
        var applied = subject.Apply(name, areaId);

        return applied.IsFailure
            ? Result.Failure<Subject>(applied.Error!)
            : Result.Success(subject);
    }

    public Result Update(string? name, Guid? areaId) => Apply(name, areaId);

    private Result Apply(string? name, Guid? areaId)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length < CatalogLimits.NameMinLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.SubjectNameRequired, ErrorKind.Validation));
        }

        if (trimmed.Length > CatalogLimits.SubjectNameMaxLength)
        {
            return Result.Failure(new Error(CatalogErrorCodes.SubjectNameTooLong, ErrorKind.Validation));
        }

        Name = trimmed;
        NormalizedName = CatalogText.Normalize(trimmed);
        AreaId = areaId;

        return Result.Success();
    }
}
