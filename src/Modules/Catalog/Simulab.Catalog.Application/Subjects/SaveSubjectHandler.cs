using Simulab.Catalog.Contracts;
using Simulab.Catalog.Domain.Entities;
using Simulab.SharedKernel.Results;

namespace Simulab.Catalog.Application.Subjects;

/// <summary>
/// Creates a subject or replaces the fields of an existing one (F-79, UC2, UC3). The shape rules belong to
/// the entity; the ones that need the tables — the area exists, the name is free — belong here. The unique
/// index is the real guarantee; the checks turn the common case into a clean answer.
/// </summary>
public sealed class SaveSubjectHandler(ISubjectStore store, ISubjectQueries queries)
{
    public async Task<Result<SubjectResponse>> HandleAsync(
        Guid? id,
        SaveSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var existing = id is { } existingId ? await store.FindAsync(existingId, cancellationToken) : null;
        if (id is not null && existing is null)
        {
            return Result.Failure<SubjectResponse>(new Error(CatalogErrorCodes.SubjectNotFound, ErrorKind.NotFound));
        }

        // Shape first, then the area and the name against the tables.
        var candidate = Subject.Create(request.Name, request.AreaId);
        if (candidate.IsFailure)
        {
            return Result.Failure<SubjectResponse>(candidate.Error!);
        }

        if (request.AreaId is { } areaId && !await store.AreaExistsAsync(areaId, cancellationToken))
        {
            return Result.Failure<SubjectResponse>(new Error(CatalogErrorCodes.SubjectAreaInvalid, ErrorKind.Validation));
        }

        if (await store.NameIsTakenAsync(candidate.Value.NormalizedName, id, cancellationToken))
        {
            return Result.Failure<SubjectResponse>(new Error(CatalogErrorCodes.SubjectNameTaken, ErrorKind.Conflict));
        }

        var subject = existing ?? candidate.Value;
        if (existing is null)
        {
            store.Add(subject);
        }
        else
        {
            // The same validation ran on the candidate a moment ago, so this one cannot fail.
            subject.Update(request.Name, request.AreaId);
        }

        // Another writer may have committed the same name since the check above (B-14).
        var refused = await store.TrySaveChangesAsync(cancellationToken);
        if (refused is not null)
        {
            return Result.Failure<SubjectResponse>(refused);
        }

        // The answer carries the area code and the topic count, which the entity does not hold.
        var saved = await queries.FindAsync(subject.Id, cancellationToken);

        return Result.Success(saved ?? new SubjectResponse(subject.Id, subject.Name, subject.AreaId, null, 0));
    }
}
